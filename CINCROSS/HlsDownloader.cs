using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace CINCROSS
{
    public class HlsDownloader : IDisposable
    {
        private readonly string _masterUrl;
        private readonly HttpClient _http;
        private string? _variantUrl;
        private List<string> _segments = new();
        private int _segIndex;
        private const int SEGMENTS_PER_BATCH = 20;

        public HlsDownloader(string url)
        {
            _masterUrl = url;
            var handler = new HttpClientHandler();
            handler.AllowAutoRedirect = true;
            handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            _http = new HttpClient(handler);
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            _http.Timeout = TimeSpan.FromSeconds(15);
        }

        public string? CurrentVariantUrl => _variantUrl;

        public async Task<bool> LoadPlaylistAsync()
        {
            try
            {
                if (_variantUrl == null)
                {
                    Log($"Fetching master: {_masterUrl}");
                    string master = await _http.GetStringAsync(_masterUrl).ConfigureAwait(false);
                    foreach (var line in master.Split('\n'))
                    {
                        var t = line.Trim();
                        if (!string.IsNullOrEmpty(t) && !t.StartsWith("#"))
                        {
                            _variantUrl = new Uri(new Uri(_masterUrl), t).ToString();
                            Log($"Variant URL: {_variantUrl}");
                            break;
                        }
                    }
                    if (_variantUrl == null) { Log("No variant URL"); return false; }
                }

                Log($"Fetching variant: {_variantUrl}");
                string variant = await _http.GetStringAsync(_variantUrl).ConfigureAwait(false);
                _segments.Clear();
                _segIndex = 0;
                foreach (var line in variant.Split('\n'))
                {
                    var t = line.Trim();
                    if (!string.IsNullOrEmpty(t) && !t.StartsWith("#"))
                        _segments.Add(t);
                }
                Log($"Found {_segments.Count} segments, first={(_segments.Count>0?_segments[0]:"(none)")}");
                return _segments.Count > 0;
            }
            catch (Exception ex)
            {
                Log($"LoadPlaylist error: {ex.GetType().Name}: {ex.Message}");
                return false;
            }
        }

        public async Task<string?> DownloadBatchAsync(string outputFile, int maxSegments = SEGMENTS_PER_BATCH)
        {
            if (_segments.Count == 0)
                if (!await LoadPlaylistAsync().ConfigureAwait(false)) return null;

            int firstSegIx = _segIndex;
            int count = Math.Min(maxSegments, _segments.Count - _segIndex);
            if (count <= 0) return null;

            var baseVar = new Uri(_variantUrl ?? _masterUrl);
            var tempFiles = new List<string>();

            for (int i = 0; i < count; i++)
            {
                string segRel = _segments[_segIndex++];
                var segUri = new Uri(baseVar, segRel);
                try
                {
                    Log($"Downloading segment {_segIndex}/{_segments.Count}: {segUri}");
                    byte[] data = await _http.GetByteArrayAsync(segUri).ConfigureAwait(false);
                    Log($"  Got {data.Length} bytes");
                    byte[] audio = (_segIndex - firstSegIx == 1) ? data : StripId3(data);
                    string partFile = outputFile + ".part" + i;
                    File.WriteAllBytes(partFile, audio);
                    tempFiles.Add(partFile);
                }
                catch (Exception ex)
                {
                    Log($"  Segment download failed: {ex.Message}");
                    break;
                }
            }

            if (tempFiles.Count == 0) return null;

            Log($"Merging {tempFiles.Count} parts into {outputFile}");
            using (var outStream = File.Create(outputFile))
            {
                foreach (var part in tempFiles)
                {
                    byte[] partData = File.ReadAllBytes(part);
                    outStream.Write(partData, 0, partData.Length);
                    File.Delete(part);
                }
            }
            Log($"Written {new FileInfo(outputFile).Length} bytes to {outputFile}");
            return outputFile;
        }

        public bool HasMoreSegments => _segIndex < _segments.Count;
        public int RemainingSegments => _segments.Count - _segIndex;
        public int DownloadedSegments => _segIndex;

        private static byte[] StripId3(byte[] data)
        {
            int off = 0;
            while (off <= data.Length - 10 && data[off] == 0x49 && data[off + 1] == 0x44 && data[off + 2] == 0x33)
            {
                int sz = (data[off + 6] << 21) | (data[off + 7] << 14) | (data[off + 8] << 7) | data[off + 9];
                off += 10 + sz;
            }
            if (off > 0)
            {
                var stripped = new byte[data.Length - off];
                Array.Copy(data, off, stripped, 0, stripped.Length);
                return stripped;
            }
            return data;
        }

        private static void Log(string msg)
        {
            try
            {
                File.AppendAllText(
                    Path.Combine(Path.GetTempPath(), "hlsplayer_log.txt"),
                    $"{DateTime.Now:HH:mm:ss.fff} [HlsDownloader] {msg}{Environment.NewLine}");
            }
            catch (Exception ex) { Logger.Error("HlsDownloader.Log", ex); }
        }

        public void Dispose()
        {
            _http.Dispose();
        }
    }
}
