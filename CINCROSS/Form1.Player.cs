using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CINCROSS;

partial class Form1
{
	private dynamic? _wmp;

	private dynamic? _wmpAlarm;

	private Timer? _metaTimer;

	private Timer? _m3u8WatchTimer;

	private HlsPlayer? _hlsPlayer;

	private bool _isHlsStream;

	private string? _lastHlsUrl;

	private bool _isPlaying;

	private int _playGen;

	private string _currentM3uName = "";
	private string _lastTitle = "";
	private string _lastArtist = "";

	private List<string> _m3uFiles = new List<string>();

	private readonly int _currentM3uIndex = 0;

	private int _currentVolume = 3;

	private void SafeSetUi(Action action)
	{
		try
		{
			if (IsHandleCreated && InvokeRequired)
				BeginInvoke(action);
			else
				action();
		}
		catch (Exception ex)
		{
			Logger.Error("Form1.SafeSetUi", ex);
		}
	}

	private void InitPlayer()
	{
		try
		{
			Type? type = Type.GetTypeFromProgID("WMPlayer.OCX.7");
			if (type != null)
			{
				_wmp = Activator.CreateInstance(type);
				_wmp!.settings.autoStart = true;
				_m3u8WatchTimer = new Timer
				{
					Interval = 3000
				};
				_m3u8WatchTimer.Tick += delegate
				{
					if (_wmp == null) return;
					try
					{
						int state = (int)_wmp.playState;
						string url = _wmp.URL ?? "";
						if (state == 1 || state == 8)
						{
							if (url.EndsWith(".m3u8", StringComparison.OrdinalIgnoreCase))
							{
								string saved = url;
								_wmp.URL = saved;
								_wmp.controls.play();
							}
						}
					}
					catch (Exception ex) { Logger.Error("Form1._m3u8WatchTimer", ex); }
				};
			}
		}
		catch (Exception ex)
		{
			Logger.Error("Form1.InitPlayer.WMP", ex);
		}
		_metaTimer = new Timer
		{
			Interval = 2000
		};
		_metaTimer.Tick += delegate
		{
			UpdateMetadata();
		};
		try
		{
			Type? alarmType = Type.GetTypeFromProgID("WMPlayer.OCX.7");
			if (alarmType != null)
			{
				_wmpAlarm = Activator.CreateInstance(alarmType);
				_wmpAlarm!.settings.autoStart = false;
			}
		}
		catch (Exception ex)
		{
			Logger.Error("Form1.InitPlayer.WMPAlarm", ex);
		}
		try
		{
			_hlsPlayer = new HlsPlayer();
			_hlsPlayer.MediaChanged += () =>
			{
				if (!_isHlsStream || _hlsPlayer == null) return;
				SafeSetUi(() =>
				{
					string? title = _hlsPlayer!.CurrentTitle;
					string? artist = _hlsPlayer.CurrentArtist;
					if (!string.IsNullOrEmpty(title) && title != _lastTitle)
					{
						_lastTitle = title;
						_lastArtist = artist ?? "";
						if (string.IsNullOrEmpty(_currentCassetteTitle))
							lblM3uTitle.Text = Path.GetFileNameWithoutExtension(_lastHlsUrl ?? "").ToUpper();
						else
							lblM3uTitle.Text = _currentCassetteTitle;
						lblMetadata.Text = title.ToUpper();
						lblExtraMetadata.Text = string.IsNullOrEmpty(artist) ? "" : artist.ToUpper();
					}
					else if (title == _lastTitle && !string.IsNullOrEmpty(artist) && artist != _lastArtist)
					{
						_lastArtist = artist;
						lblExtraMetadata.Text = artist.ToUpper();
					}
				});
			};
			_hlsPlayer.Error += msg =>
			{
				SafeSetUi(() =>
				{
					try { lblExtraMetadata.Text = msg.ToUpper(); } catch (Exception innerEx) { Logger.Error("Form1.HlsPlayer.Error", innerEx); }
				});
			};
		}
		catch (Exception ex)
		{
			try { lblExtraMetadata.Text = $"VLC: {ex.Message}".ToUpper(); } catch (Exception innerEx) { Logger.Error("Form1.HlsPlayer.Init", innerEx); }
		}
	}

	private void UpdateMetadata()
	{
		try
		{
			if (_isHlsStream)
			{
				_hlsPlayer?.PollMetadata();
				return;
			}
		}
		catch (Exception ex)
		{
			Logger.Error("Form1.UpdateMetadata.Hls", ex);
		}
		if (_wmp == null)
		{
			return;
		}
		try
		{
			dynamic media = _wmp.currentMedia;
			if (!((media != null) ? true : false))
			{
				return;
			}
			string title = media.getItemInfo("Title");
			string artist = media.getItemInfo("Author");
			string album = media.getItemInfo("Album");
			string genre = media.getItemInfo("Genre");
			string src = media.sourceURL;
			if (string.IsNullOrEmpty(title))
			{
				if (!string.IsNullOrEmpty(src))
				{
					try
					{
						title = Path.GetFileNameWithoutExtension(src);
					}
					catch
					{
						title = src;
					}
				}
				if (string.IsNullOrEmpty(title))
				{
					title = media.name;
				}
			}
			if (!string.IsNullOrEmpty(title) && title != _lastTitle)
			{
				_lastTitle = title;
				lblM3uTitle.Text = string.IsNullOrEmpty(_currentCassetteTitle) ? _currentM3uName : _currentCassetteTitle;
				lblMetadata.Text = title.ToUpper();
				string line2 = ((!string.IsNullOrEmpty(artist)) ? artist : genre);
				if (!string.IsNullOrEmpty(album))
				{
					line2 = ((!string.IsNullOrEmpty(line2)) ? (line2 + " - " + album) : album);
				}
				lblExtraMetadata.Text = line2.ToUpper();
			}
		}
		catch (Exception ex)
		{
			Logger.Error("Form1.UpdateMetadata", ex);
		}
	}

	private async Task PlayM3uAsync(string path)
	{
		int gen = ++_playGen;
		_isHlsStream = path.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
			|| path.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
			|| path.EndsWith(".m3u8", StringComparison.OrdinalIgnoreCase);
		if (_isHlsStream)
		{
			_metaTimer?.Stop();
			_m3u8WatchTimer?.Stop();
			try { _wmp?.controls.stop(); } catch (Exception ex) { Logger.Error("Form1.PlayM3u.StopWmp", ex); }
			if (_hlsPlayer != null)
				await _hlsPlayer.PlayAsync(path);
			_lastHlsUrl = path;
			_currentM3uName = Path.GetFileNameWithoutExtension(path).ToUpper();
			_lastTitle = "";
			_lastArtist = "";
			if (string.IsNullOrEmpty(_currentCassetteTitle))
				lblM3uTitle.Text = _currentM3uName;
			lblMetadata.Text = "";
			lblExtraMetadata.Text = "";
			lblM3uTitle.Visible = true;
			lblMetadata.Visible = true;
			lblExtraMetadata.Visible = true;
			_isPlaying = true;
			SetVolumePreset(_currentVolume);
			_metaTimer?.Start();
			return;
		}
		_hlsPlayer?.Stop();
		if (_wmp == null)
		{
			return;
		}
		try
		{
			_metaTimer?.Stop();
			_m3u8WatchTimer?.Stop();
			_wmp.URL = path;
			_wmp.controls.play();
			_currentM3uName = Path.GetFileNameWithoutExtension(path).ToUpper();
			_lastTitle = "";
			_lastArtist = "";
			if (string.IsNullOrEmpty(_currentCassetteTitle))
				lblM3uTitle.Text = _currentM3uName;
			lblMetadata.Text = "";
			lblExtraMetadata.Text = "";
			lblM3uTitle.Visible = true;
			lblMetadata.Visible = true;
			lblExtraMetadata.Visible = true;
			_metaTimer?.Start();
			_isPlaying = true;
			SetVolumePreset(_currentVolume);
			_ = WatchWmpThenFallbackAsync(path, gen);
		}
		catch
		{
			lblMetadata.Text = "";
			lblExtraMetadata.Text = "";
		}
	}

	private async Task WatchWmpThenFallbackAsync(string url, int gen)
	{
		await Task.Delay(10000);
		if (gen != _playGen || _isHlsStream || _wmp == null) return;
		int state;
		try
		{
			state = (int)_wmp!.playState;
		}
		catch (Exception ex)
		{
			Logger.Error("Form1.WatchWmpThenFallback", ex);
			return;
		}
		bool alive = state == 3 || state == 6 || state == 7 || state == 11;
		if (alive) return;
		try { lblExtraMetadata.Text = "SINTONIZANDO..."; } catch (Exception ex) { Logger.Error("Form1.WatchWmpThenFallback.lbl", ex); }
		try { _wmp.controls.stop(); } catch (Exception ex) { Logger.Error("Form1.WatchWmpThenFallback.stop", ex); }
		_metaTimer?.Stop();
		_m3u8WatchTimer?.Stop();
		_hlsPlayer?.Stop();
		_isHlsStream = true;
		_lastHlsUrl = url;
		_lastTitle = "";
		_lastArtist = "";
		try
		{
			lblMetadata.Text = "";
			lblExtraMetadata.Text = "";
			lblM3uTitle.Visible = true;
			lblMetadata.Visible = true;
			lblExtraMetadata.Visible = true;
		}
		catch (Exception ex) { Logger.Error("Form1.WatchWmpThenFallback.ui", ex); }
		if (_hlsPlayer != null)
			await _hlsPlayer.PlayAsync(url);
		if (gen != _playGen) return;
		_isPlaying = true;
		SetVolumePreset(_currentVolume);
		_metaTimer?.Start();
	}

	private void SetVolumePreset(int percent)
	{
		_currentVolume = percent;
		int vol = percent * 100 / 15;
		if (_isHlsStream)
		{
			if (_hlsPlayer != null) _hlsPlayer.Volume = vol;
		}
		else
		{
			if (_wmp != null) _wmp.settings.volume = vol;
		}
		UpdateVolumePresetMarks(percent);
		UpdateVolumeVisual(percent);
	}

	private void UpdateVolumePresetMarks(int percent)
	{
		btnVolLow.Tag = percent == 3;
		btnVolMid.Tag = percent == 9;
		btnVolMax.Tag = percent == 15;
		btnVolLow.Font = new Font("Segoe UI", 7f, FontStyle.Bold);
		btnVolMid.Font = new Font("Segoe UI", 7f, FontStyle.Bold);
		btnVolMax.Font = new Font("Segoe UI", 7f, FontStyle.Bold);
		btnVolLow.Invalidate();
		btnVolMid.Invalidate();
		btnVolMax.Invalidate();
		if (btnAbcA != null)
		{
			btnAbcA.Tag = percent == 15;
			btnAbcB.Tag = percent == 9;
			btnAbcC.Tag = percent == 3;
			btnAbcA.Invalidate();
			btnAbcB.Invalidate();
			btnAbcC.Invalidate();
		}
	}

	private void UpdateVolumeFromMouse(int mouseX)
	{
		int x = Math.Max(0, Math.Min(mouseX - pnlVolumeLine.Left, pnlVolumeLine.Width));
		int thumbCenter = pnlVolumeThumb.Width / 2;
		pnlVolumeThumb.Left = x - thumbCenter;
		double raw = (double)x / (double)pnlVolumeLine.Width;
		int volume = (int)(Math.Max(0.03, Math.Min(0.15, raw)) * 100.0);
		_currentVolume = volume;
		int vol = volume * 100 / 15;
		if (_isHlsStream)
		{
			if (_hlsPlayer != null) _hlsPlayer.Volume = vol;
		}
		else
		{
			if (_wmp != null) _wmp.settings.volume = vol;
		}
		UpdateVolumePresetMarks(volume);
	}

	private void UpdateVolumeVisual(int volumePercent)
	{
		try
		{
			double normalized = (double)(volumePercent - 3) / 12.0;
			normalized = Math.Max(0, Math.Min(1, normalized));
			int x = (int)(normalized * (double)pnlVolumeLine.Width);
			int thumbCenter = pnlVolumeThumb.Width / 2;
			pnlVolumeThumb.Left = x - thumbCenter;
		}
		catch (Exception ex)
		{
			Logger.Error("Form1.UpdateVolumeVisual", ex);
		}
	}
	
	private void StopM3u()
	{
		_playGen++;
		_hlsPlayer?.Stop();
		_metaTimer?.Stop();
		_m3u8WatchTimer?.Stop();
		try
		{
			_wmp?.controls.stop();
		}
		catch (Exception ex)
		{
			Logger.Error("Form1.StopM3u", ex);
		}
		_lastTitle = "";
		_lastArtist = "";
		lblMetadata.Text = "";
		lblExtraMetadata.Text = "";
		_isPlaying = false;
	}

	private void PlayDoneSound()
	{
		if (_wmp == null)
		{
			return;
		}
		string donePath = Path.Combine(PathHelper.GetFilesDir(), "mp3", "DONE.mp3");
		if (File.Exists(donePath))
		{
			_wmp.URL = donePath;
			_wmp.controls.play();
			lblMetadata.Text = "";
		}
	}

	private async Task PlayAlarmSound()
	{
		if (_wmpAlarm == null) return;

		string alarmPath = Path.Combine(PathHelper.GetFilesDir(), "mp3", "alarm000.mp3");
		if (!File.Exists(alarmPath)) return;

		for (int i = 0; i < 2; i++)
		{
			try
			{
				_wmpAlarm.URL = alarmPath;
				_wmpAlarm.settings.volume = 40;
				_wmpAlarm.controls.play();

				await Task.Delay(200);

				for (int j = 0; j < 600; j++)
				{
					await Task.Delay(50);
					try
					{
						int state = (int)_wmpAlarm.playState;
						if (state == 1 || state == 0) break;
					}
					catch { break; }
				}
			}
			catch (Exception ex)
			{
				Logger.Error("Form1.PlayAlarmSound", ex);
				break;
			}
		}
	}

	private void StopAlarm()
	{
		if (_wmpAlarm == null) return;
		try { _wmpAlarm.controls.stop(); }
		catch (Exception ex) { Logger.Error("Form1.StopAlarm", ex); }
	}
}
