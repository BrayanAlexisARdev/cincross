using LibVLCSharp.Shared;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace CINCROSS
{
    public class HlsPlayer : IDisposable
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool SetDllDirectory(string lpPathName);

        private LibVLC? _libVlc;
        private MediaPlayer? _mediaPlayer;
        private Media? _currentMedia;
        private HlsDownloader? _hlsDownloader;
        private string? _currentTempFile;
        private bool _disposed;
        private bool _initialized;
        private string _logPath;
        private bool _isHls;
        private bool _stoppedGuard;

        public MediaPlayer? Player => _mediaPlayer;
        public bool IsPlaying => _mediaPlayer?.IsPlaying ?? false;
        public string? CurrentTitle { get; private set; }
        public string? CurrentArtist { get; private set; }

        public int Volume
        {
            get => _mediaPlayer?.Volume ?? 50;
            set
            {
                if (_mediaPlayer != null)
                    _mediaPlayer.Volume = Math.Clamp(value, 0, 100);
            }
        }

        public event Action? MediaChanged;
        public event Action? Stopped;
        public event Action<string>? Error;

        public HlsPlayer()
        {
            _logPath = Path.Combine(Path.GetTempPath(), "hlsplayer_log.txt");
            Log("HlsPlayer constructor start");

            try
            {
                string nativeDir = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "libvlc",
                    Environment.Is64BitProcess ? "win-x64" : "win-x86");

                Log($"Trying native dir: {nativeDir}");
                Log($"libvlc.dll exists: {File.Exists(Path.Combine(nativeDir, "libvlc.dll"))}");

                if (!File.Exists(Path.Combine(nativeDir, "libvlc.dll")))
                {
                    string altDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "libvlc", "win-x64");
                    Log($"Alt dir: {altDir} exists: {Directory.Exists(altDir)}");
                    if (File.Exists(Path.Combine(altDir, "libvlc.dll")))
                        nativeDir = altDir;
                }

                SetDllDirectory(nativeDir);
                Log("SetDllDirectory OK");

                string pluginPath = Path.Combine(nativeDir, "plugins");
                Log($"Plugin path: {pluginPath} exists: {Directory.Exists(pluginPath)}");
                Environment.SetEnvironmentVariable("VLC_PLUGIN_PATH", pluginPath);

_libVlc = new LibVLC("--no-video",
					"--verbose=2",
					$"--plugin-path={pluginPath}",
					"--no-plugins-cache");
                Log("LibVLC created OK");

                _libVlc.Log += (s, e) =>
                {
                    Log($"VLC [{e.Level}] {e.Module}: {e.Message}");
                };

                _mediaPlayer = new MediaPlayer(_libVlc);
                _mediaPlayer.MediaChanged += OnMediaChanged;
                _mediaPlayer.Stopped += OnStopped;
                _mediaPlayer.Playing += (s, e) => Log("MediaPlayer Playing");
                _mediaPlayer.Paused += (s, e) => Log("MediaPlayer Paused");
                _mediaPlayer.EncounteredError += (s, e) =>
                {
                    Log("MediaPlayer EncounteredError");
                    Error?.Invoke("Error en reproduccion VLC");
                };
                _mediaPlayer.TimeChanged += (s, e) => { };
                Log("MediaPlayer created OK");
                _initialized = true;
            }
            catch (Exception ex)
            {
                Log($"CONSTRUCTOR ERROR: {ex.GetType().Name}: {ex.Message}");
                Log($"StackTrace: {ex.StackTrace}");
                _initialized = false;
            }
        }

        public async Task PlayAsync(string url)
        {
            Log($"Play({url})");
            if (_disposed) { Log("  -> disposed, abort"); return; }
            if (!_initialized) { Log("  -> not initialized, abort"); Error?.Invoke("VLC NO INICIALIZADO"); return; }

            Stop();

            try
            {
                bool isHttp = url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                              url.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
                bool looksLikeHls = url.EndsWith(".m3u8", StringComparison.OrdinalIgnoreCase) ||
                                    url.EndsWith(".m3u", StringComparison.OrdinalIgnoreCase);

                if (isHttp && looksLikeHls)
                {
                    _hlsDownloader = new HlsDownloader(url);
                    if (!await _hlsDownloader.LoadPlaylistAsync().ConfigureAwait(false))
                    {
                        Log("Failed to load HLS playlist -> falling back to direct stream");
                        _hlsDownloader.Dispose();
                        _hlsDownloader = null;
                        _isHls = false;
                        PlayDirect(url, isHttp);
                        return;
                    }
                    _isHls = true;
                    await PlayNextBatchAsync().ConfigureAwait(false);
                }
                else
                {
                    _isHls = false;
                    PlayDirect(url, isHttp);
                }
            }
            catch (Exception ex)
            {
                Log($"Play ERROR: {ex.GetType().Name}: {ex.Message}");
                Error?.Invoke($"ERROR: {ex.Message}");
            }
        }

        private void PlayDirect(string url, bool isHttp)
        {
            if (_libVlc == null || _mediaPlayer == null)
            {
                Log("PlayDirect: VLC not initialized");
                Error?.Invoke("VLC NO INICIALIZADO");
                return;
            }
            _currentMedia?.Dispose();
            _currentMedia = null;
            _currentMedia = isHttp
                ? new Media(_libVlc, url, FromType.FromLocation)
                : new Media(_libVlc, url);
            if (isHttp)
                _currentMedia.AddOption(":http-user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
            bool result = _mediaPlayer.Play(_currentMedia);
            Log($"  Play() result: {result}, State: {_mediaPlayer.State}");
            if (!result)
            {
                Error?.Invoke("FALLO AL REPRODUCIR");
            }
        }

        private async Task<bool> PlayNextBatchAsync()
        {
            if (_hlsDownloader == null) return false;

            CleanupTempFile();

            if (_hlsDownloader.RemainingSegments == 0)
            {
                Log("Refreshing playlist for more segments...");
                if (!await _hlsDownloader.LoadPlaylistAsync().ConfigureAwait(false))
                {
                    Log("No new segments");
                    return false;
                }
            }

            if (_hlsDownloader.RemainingSegments == 0)
            {
                Log("Still no segments");
                return false;
            }

            string tempDir = Path.Combine(Path.GetTempPath(), "x5ver_hls");
            Directory.CreateDirectory(tempDir);
            string tempFile = Path.Combine(tempDir, $"stream_{DateTime.Now.Ticks}.aac");

            string? result = await _hlsDownloader.DownloadBatchAsync(tempFile).ConfigureAwait(false);
            if (string.IsNullOrEmpty(result))
            {
                Log("DownloadBatch returned empty, retrying with fresh playlist...");
                if (!await _hlsDownloader.LoadPlaylistAsync().ConfigureAwait(false))
                    return false;
                result = await _hlsDownloader.DownloadBatchAsync(tempFile).ConfigureAwait(false);
                if (string.IsNullOrEmpty(result))
                {
                    Log("Still empty after retry");
                    return false;
                }
            }

            _currentTempFile = result;
            Log($"Playing temp file: {_currentTempFile}");

            if (_libVlc == null || _mediaPlayer == null)
            {
                Log("PlayNextBatch: VLC not initialized");
                Error?.Invoke("VLC NO INICIALIZADO");
                return false;
            }
            _currentMedia?.Dispose();
            _currentMedia = new Media(_libVlc, _currentTempFile);
            bool playResult = _mediaPlayer.Play(_currentMedia);
            Log($"  Play() result: {playResult}, State: {_mediaPlayer.State}");
            if (!playResult)
            {
                Error?.Invoke("FALLO AL REPRODUCIR ARCHIVO TEMPORAL");
                return false;
            }
            return true;
        }

        private async void OnStopped(object? sender, EventArgs e)
        {
            if (_stoppedGuard) return;
            _stoppedGuard = true;
            try
            {
                Log("MediaPlayer Stopped");
                if (_isHls && _hlsDownloader != null)
                {
                    Log("HLS mode, checking for next batch...");
                    if (!await PlayNextBatchAsync().ConfigureAwait(false))
                    {
                        Log("No more batches, firing final Stop");
                        Stopped?.Invoke();
                    }
                }
                else
                {
                    Stopped?.Invoke();
                }
            }
            catch (Exception ex)
            {
                Log($"OnStopped ERROR: {ex.GetType().Name}: {ex.Message}");
            }
            finally
            {
                _stoppedGuard = false;
            }
        }

        public void Stop()
        {
            if (!_disposed)
            {
                _isHls = false;
                _mediaPlayer?.Stop();
                _currentMedia?.Dispose();
                _currentMedia = null;
                _hlsDownloader?.Dispose();
                _hlsDownloader = null;
                CleanupTempFile();
            }
        }

        private void CleanupTempFile()
        {
            try
            {
                if (_currentTempFile != null && File.Exists(_currentTempFile))
                {
                    File.Delete(_currentTempFile);
                    Log($"Deleted temp file: {_currentTempFile}");
                }
            }
            catch (Exception ex)
            {
                Log($"Cleanup temp file error: {ex.Message}");
            }
            _currentTempFile = null;
        }

        private void OnMediaChanged(object? sender, MediaPlayerMediaChangedEventArgs e)
        {
            Log("OnMediaChanged");
            if (e.Media != null)
            {
                string? title = e.Media.Meta(MetadataType.Title);
                string? artist = e.Media.Meta(MetadataType.Artist);
                Log($"  Title: {title}, Artist: {artist}");

                if (!string.IsNullOrEmpty(title))
                    CurrentTitle = title;
                if (!string.IsNullOrEmpty(artist))
                    CurrentArtist = artist;

                MediaChanged?.Invoke();
            }
        }

        public void PollMetadata()
		{
			try
			{
				if (_mediaPlayer == null || _currentMedia == null || !_mediaPlayer.IsPlaying)
				{
					return;
				}
				string? title = _currentMedia.Meta(MetadataType.Title);
				string? artist = _currentMedia.Meta(MetadataType.Artist);
				bool changed = false;
				if (!string.IsNullOrEmpty(title) && title != CurrentTitle)
				{
					CurrentTitle = title;
					changed = true;
				}
				if (!string.IsNullOrEmpty(artist) && artist != CurrentArtist)
				{
					CurrentArtist = artist;
					changed = true;
				}
				if (changed)
				{
					MediaChanged?.Invoke();
				}
			}
			catch (Exception ex)
			{
				Log($"PollMetadata ERROR: {ex.Message}");
			}
		}

		private void Log(string msg)
        {
            try
            {
                File.AppendAllText(_logPath, $"{DateTime.Now:HH:mm:ss.fff} [HlsPlayer] {msg}{Environment.NewLine}");
            }
            catch (Exception ex) { Logger.Error("HlsPlayer.Log", ex); }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _isHls = false;
                _mediaPlayer?.Stop();
                _currentMedia?.Dispose();
                _hlsDownloader?.Dispose();
                _mediaPlayer?.Dispose();
                _libVlc?.Dispose();
                CleanupTempFile();
                Log("HlsPlayer disposed");
            }
        }
    }
}
