using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace CINCROSS;

partial class Form1
{
	private List<Image> _cassetteImages = new List<Image>();

	private Image? _playerImage;

	private List<CassetteData> _cassettes = new List<CassetteData>();

	private TagsBrowserForm? _tagsBrowser;

	private int _currentCassetteIndex;
	private string _currentCassetteTitle = "";

	private readonly Dictionary<string, Image> _imageCache = new Dictionary<string, Image>(StringComparer.OrdinalIgnoreCase);

	private readonly object _imageCacheLock = new object();

	private static Image? LoadFromDisk(string path)
	{
		try
		{
			return PathHelper.LoadImage(path);
		}
		catch (Exception ex)
		{
			Logger.Error("Form1.LoadFromDisk", ex);
			return null;
		}
	}

	private Image? GetCachedImage(string path)
	{
		if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
		lock (_imageCacheLock)
		{
			if (_imageCache.TryGetValue(path, out Image? img)) return img;
			Image? loaded = LoadFromDisk(path);
			if (loaded != null) _imageCache[path] = loaded;
			return loaded;
		}
	}

	private void ReplacePlayerImage(Image newImage)
	{
		_playerImage = newImage;
		picPlayer.Image = newImage;
		picPlayer.SizeMode = PictureBoxSizeMode.Zoom;
		picPlayer.Size = new Size(198, 125);
		picPlayer.Location = new Point(4, 19);
	}

	private Image? GetCassetteImageFromM3u(string m3uPath, int indexFallback)
	{
		try
		{
			if (File.Exists(m3uPath))
			{
				string[] array = File.ReadAllLines(m3uPath);
				foreach (string line in array)
				{
					if (line.StartsWith("#CASSETTE:", StringComparison.OrdinalIgnoreCase))
					{
						string imgName = line.Substring("#CASSETTE:".Length).Trim();
						string fullPath = Path.Combine(Path.GetDirectoryName(m3uPath) ?? "", imgName);
						Image? cached = GetCachedImage(fullPath);
						if (cached != null)
						{
							return cached;
						}
					}
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Error("Form1.GetCassetteImageFromM3u", ex);
		}
		if (_cassetteImages.Count > 0)
		{
			return _cassetteImages[indexFallback % _cassetteImages.Count];
		}
		return null;
	}

	private void LoadCurrentM3u()
	{
		if (_m3uFiles.Count != 0)
		{
			string path = _m3uFiles[_currentM3uIndex];
			_ = PlayM3uAsync(path);
			_currentM3uName = Path.GetFileNameWithoutExtension(path).ToUpper();
			lblM3uTitle.Text = _currentM3uName;
			lblMetadata.Text = "";
			Image? currentImg = GetCassetteImageFromM3u(path, _currentM3uIndex);
			if (currentImg != null)
			{
				ReplacePlayerImage(currentImg);
			}
		}
	}

	private void LoadCassetteMaster()
	{
		string m3uDir = PathHelper.GetM3uDir();
		string masterPath = Path.Combine(m3uDir, "CASS_master.txt");
		if (!File.Exists(masterPath)) return;

		_cassettes.Clear();
		CassetteData? current = null;

		foreach (string line in File.ReadAllLines(masterPath))
		{
			string trimmed = line.Trim();
			if (trimmed.StartsWith(";") || string.IsNullOrEmpty(trimmed)) continue;

			if (trimmed.StartsWith("[CASSETTE"))
			{
				if (current != null) _cassettes.Add(current);
				current = new CassetteData();
				continue;
			}

			if (current != null && trimmed.Contains(":"))
			{
				int colonIdx = trimmed.IndexOf(':');
				string key = trimmed.Substring(0, colonIdx).Trim().ToUpper();
				string value = trimmed.Substring(colonIdx + 1).Trim();

				switch (key)
				{
					case "TITULO": current.Titulo = value; break;
					case "IMAGEN": current.Imagen = value; break;
					case "CONTENIDO": current.Contenido = value; break;
					case "COLOR": current.Color = value; break;
					case "PANTALLA_GIF": current.PantallaGif = value; break;
					case "TEMA_TV": current.TemaTV = value; break;
					case "TAGS": current.Tags = value; break;
				}
			}
		}
		if (current != null) _cassettes.Add(current);
	}

	private static string ResolveImgPath(string fileName) => PathHelper.ResolveImg(fileName);

	private Image? LoadCassetteImage(string imgName)
	{
		if (string.IsNullOrEmpty(imgName)) return null;
		return GetCachedImage(ResolveImgPath(imgName));
	}

	private void ApplyCassette(int index)
	{
		if (index < 0 || index >= _cassettes.Count) return;
		ApplyCassetteFunctional(index);
		if (_slideTimer is { Enabled: true }) return;
		CassetteData cass = _cassettes[index];
		if (!string.IsNullOrEmpty(cass.Imagen))
		{
			Image? img = GetCachedImage(ResolveImgPath(cass.Imagen));
			if (img != null)
			{
				ReplacePlayerImage(img);
			}
		}
	}

	private void ApplyCassetteFunctional(int index)
	{
		if (index < 0 || index >= _cassettes.Count) return;
		CassetteData cass = _cassettes[index];
		_currentCassetteIndex = index;
		AdvanceEqStyle();

		txtCassetteNum.Text = (index + 1).ToString();
		lblCassetteTotal.Text = $"/{_cassettes.Count}";

		lblM3uTitle.Text = cass.Titulo.ToUpper();
		_currentCassetteTitle = cass.Titulo.ToUpper();
		UpdateCassetteHeaderText();
		lblMetadata.Text = "";
		lblExtraMetadata.Text = "";

		if (!string.IsNullOrEmpty(cass.Color))
		{
			try
			{
			Color baseColor = ColorTranslator.FromHtml(cass.Color);
				ApplyAppColor(baseColor);
			}
			catch (Exception ex) { Logger.Error("Form1.ApplyCassette.Color", ex); }
		}

		if (!string.IsNullOrEmpty(cass.PantallaGif))
		{
			string gifPath = ResolveImgPath(cass.PantallaGif);
			if (File.Exists(gifPath))
			{
				picMainDisplay.ImageLocation = gifPath;
				picMainDisplay.SizeMode = PictureBoxSizeMode.Zoom;
			}
		}

		if (!string.IsNullOrEmpty(cass.TemaTV))
		{
			string tvPath = ResolveImgPath(cass.TemaTV);
			Image? tvImg = GetCachedImage(tvPath);
			if (tvImg != null)
			{
				SetTimerBackground(tvImg, true);
				timerPanel.BackgroundImageLayout = ImageLayout.None;
				timerPanel.Height = tvImg.Height - 4;
				_currentTvPath = tvPath;
			}
		}

		if (!string.IsNullOrEmpty(cass.Contenido) &&
			!cass.Contenido.Equals("ARREGLAR", StringComparison.OrdinalIgnoreCase))
		{
			_ = PlayM3uAsync(cass.Contenido);
		}
	}

	private void ChangeCassette(int direction)
	{
		if (_cassettes.Count == 0) return;
		if (_slideTimer is { Enabled: true }) return;

		int newIndex = (_currentCassetteIndex + direction + _cassettes.Count) % _cassettes.Count;
		CassetteData nextCass = _cassettes[newIndex];

		ApplyCassetteFunctional(newIndex);

		Image? nextImg = null;
		if (!string.IsNullOrEmpty(nextCass.Imagen))
		{
			nextImg = GetCachedImage(ResolveImgPath(nextCass.Imagen));
		}

		if (nextImg != null)
		{
			StartFade(nextImg);
		}
	}

	private void GoToCassette(int index)
	{
		if (index < 0 || index >= _cassettes.Count) return;
		if (index == _currentCassetteIndex) return;
		if (_slideTimer is { Enabled: true }) return;

		CassetteData cass = _cassettes[index];
		ApplyCassetteFunctional(index);

		Image? nextImg = null;
		if (!string.IsNullOrEmpty(cass.Imagen))
		{
			nextImg = GetCachedImage(ResolveImgPath(cass.Imagen));
		}

		if (nextImg != null)
		{
			StartFade(nextImg);
		}
	}

	private void txtCassetteNum_KeyDown(object? sender, KeyEventArgs e)
	{
		if (e.KeyCode == Keys.Enter)
		{
			e.SuppressKeyPress = true;
			NavigateToTextBoxCassette();
		}
	}

	private void txtCassetteNum_Leave(object? sender, EventArgs e)
	{
		NavigateToTextBoxCassette();
	}

	private void NavigateToTextBoxCassette()
	{
		if (int.TryParse(txtCassetteNum.Text, out int num) && num >= 1 && num <= _cassettes.Count)
			GoToCassette(num - 1);
		else
			txtCassetteNum.Text = (_currentCassetteIndex + 1).ToString();
	}

	private void btnCassetteList_Click(object? sender, EventArgs e)
	{
		using var form = new CassetteListForm(_cassettes.ToArray(), _currentCassetteIndex);
		form.Location = new Point(Left - form.Width, Top);
		if (form.ShowDialog(this) == DialogResult.OK && form.SelectedIndex >= 0)
			GoToCassette(form.SelectedIndex);
	}

	private void ShowTagsBrowser()
	{
		if (_tagsBrowser != null && !_tagsBrowser.IsDisposed)
		{
			_tagsBrowser.Activate();
			return;
		}
		_tagsBrowser = new TagsBrowserForm(_cassettes.ToArray(), GoToCassette, Height);
		_tagsBrowser.FormClosed += (_, _) => _tagsBrowser = null;
		_tagsBrowser.Location = new Point(Left - _tagsBrowser.Width, Top);
		_tagsBrowser.Show(this);
	}

	private void WarmCassetteCache()
	{
		try
		{
			for (int i = 0; i < _cassettes.Count; i++)
			{
				CassetteData c = _cassettes[i];
				if (!string.IsNullOrEmpty(c.Imagen)) LoadCassetteImage(c.Imagen);
				if (!string.IsNullOrEmpty(c.TemaTV)) _ = GetCachedImage(ResolveImgPath(c.TemaTV));
			}
		}
		catch (Exception ex)
		{
			Logger.Error("Form1.WarmCassetteCache", ex);
		}
		try
		{
			for (int i = 0; i < _m3uFiles.Count; i++)
				GetCassetteImageFromM3u(_m3uFiles[i], i);
		}
		catch (Exception ex)
		{
			Logger.Error("Form1.WarmCassetteCache.M3u", ex);
		}
	}

	private void LayoutCassetteHeader()
	{
		lblCassetteTotal.Text = $"/{_cassettes.Count}";
		txtCassetteNum.Width = TextRenderer.MeasureText("888", txtCassetteNum.Font).Width;
		txtCassetteNum.Location = new Point(70, 3);
		lblCassetteTotal.Location = new Point(70 + txtCassetteNum.Width + 2, 6);
	}

	private void UpdateCassetteHeaderText()
	{
		lblCassettes.Text = "CASSETTES";
		lblCassetteCount.Text = GetCassetteNumberText();
		LayoutCassetteCount();
	}

	private string GetCassetteNumberText()
	{
		int total = _cassettes.Count;
		int num = total > 0 ? _currentCassetteIndex + 1 : 0;
		return $"{num}/{total}";
	}

	private void LayoutCassetteCount()
	{
		if (lblCassetteCount == null) return;
		lblCassettes.AutoSize = true;
		lblCassetteCount.AutoSize = true;
		int panelW = cassettesHeaderPanel.Width;
		int gap = 5;
		int rightPad = 2;
		int countW = lblCassetteCount.PreferredWidth;
		int maxTitleW = panelW - countW - gap - rightPad;
		if (lblCassettes.PreferredWidth > maxTitleW)
		{
			lblCassettes.Text = TruncateText(lblCassettes.Text, maxTitleW, lblCassettes.Font);
		}
		lblCassettes.AutoSize = true;
		lblCassetteCount.AutoSize = true;
		int titleW = lblCassettes.PreferredWidth;
		countW = lblCassetteCount.PreferredWidth;
		int groupW = titleW + gap + countW;
		int groupX = (panelW - groupW) / 2;
		int y = (cassettesHeaderPanel.Height - Math.Max(lblCassettes.PreferredHeight, lblCassetteCount.PreferredHeight)) / 2;
		lblCassettes.Location = new Point(groupX, y);
		lblCassetteCount.Location = new Point(groupX + titleW + gap, y);
		lblCassettes.Padding = Padding.Empty;
		lblCassetteCount.Padding = Padding.Empty;
	}

	private static string TruncateText(string text, int maxWidth, Font font)
	{
		if (string.IsNullOrEmpty(text) || TextRenderer.MeasureText(text, font).Width <= maxWidth)
			return text;
		string ellipsis = "\u2026";
		int lo = 0, hi = text.Length;
		string result = "";
		while (lo < hi)
		{
			int mid = (lo + hi + 1) / 2;
			string candidate = text.Substring(0, mid) + ellipsis;
			if (TextRenderer.MeasureText(candidate, font).Width <= maxWidth)
			{
				result = candidate;
				lo = mid;
			}
			else
			{
				hi = mid - 1;
			}
		}
		return result;
	}
}
