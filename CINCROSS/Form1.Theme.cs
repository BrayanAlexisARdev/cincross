using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Windows.Forms;

namespace CINCROSS;

partial class Form1
{
	private PrivateFontCollection _pfc = new PrivateFontCollection();

	private List<string> _themeFilePaths = new List<string>();

	private int _currentThemeImageIndex;

	private Image? _currentThemeImage;

	private readonly int _currentThemeIndex = 0;

	private readonly (Color timer, Color header, Color list, Color footer)[] _themes = new(Color, Color, Color, Color)[8]
	{
		(Color.FromArgb(60, 60, 60), Color.FromArgb(80, 80, 80), Color.FromArgb(40, 40, 40), Color.FromArgb(50, 50, 50)),
		(Color.RoyalBlue, Color.LightGray, Color.FromArgb(64, 64, 64), Color.White),
		(Color.Black, Color.FromArgb(45, 45, 45), Color.Black, Color.Black),
		(Color.White, Color.FromArgb(240, 240, 240), Color.White, Color.White),
		(Color.Crimson, Color.LightPink, Color.Maroon, Color.White),
		(Color.ForestGreen, Color.LightGreen, Color.DarkGreen, Color.White),
		(Color.Gold, Color.LightYellow, Color.DarkGoldenrod, Color.White),
		(Color.Purple, Color.Plum, Color.Indigo, Color.White)
	};

	private List<(string TvPath, string ThPath)> _themesList = new();
	private int _currentCustomThemeIndex;

	private Color _appColor = Color.Transparent;

	private string _currentColorHex = "";
	private string _currentTvPath = "";

	private void LoadCustomFont()
	{
		string dcPath = Path.Combine(PathHelper.GetFilesDir(), "typo", "dc.ttf");
		if (File.Exists(dcPath))
		{
			_pfc.AddFontFile(dcPath);
			lblHours.Font = new Font(_pfc.Families[0], 11f, FontStyle.Bold);
			lblMinutes.Font = new Font(_pfc.Families[0], 15f, FontStyle.Bold);
			lblSeconds.Font = new Font(_pfc.Families[0], 11f, FontStyle.Bold);
		}
		string[] excludes = new string[18]
		{
			"lblHours", "lblMinutes", "lblSeconds", "btnP", "btnS", "btnColor", "btnStyle", "btnImage", "btnTheme", "btnPrevM3u",
			"btnNextM3u", "btnPlayPlayer", "btnStopPlayer", "btnCassetteList", "btnFavList", "btnAddFav", "btnLearn", "btnInfo"
		};
		FontHelper.ApplyFont(this, 10f, FontStyle.Regular, excludes);
		if (FontHelper.CustomFontFamily != null)
		{
			lblTasks.Font = new Font(FontHelper.CustomFontFamily, 9f, FontStyle.Bold);
			lblTools.Font = new Font(FontHelper.CustomFontFamily, 9f, FontStyle.Bold);
			lblCassettes.Font = new Font(FontHelper.CustomFontFamily, 9f, FontStyle.Bold);
			lblCassetteCount.Font = new Font(FontHelper.CustomFontFamily, 8f, FontStyle.Bold);
			txtCassetteNum.Font = new Font(FontHelper.CustomFontFamily, 9f, FontStyle.Bold);
			lblCassetteTotal.Font = new Font(FontHelper.CustomFontFamily, 8f, FontStyle.Bold);
			lblM3uTitle.Font = new Font(FontHelper.CustomFontFamily, 8f, FontStyle.Bold);
			lblMetadata.Font = new Font(FontHelper.CustomFontFamily, 6.5f, FontStyle.Regular);
			lblExtraMetadata.Font = new Font(FontHelper.CustomFontFamily, 6f, FontStyle.Regular);
			Font miniFont = new Font(FontHelper.CustomFontFamily, 6f, FontStyle.Regular);
			btnColor.Font = miniFont;
			btnImage.Font = miniFont;
			btnTheme.Font = miniFont;
			btnLearn.Font = miniFont;
			btnAddFav.Font = miniFont;
			btnInfo.Font = miniFont;
			btnPlayPlayer.Font = miniFont;
			btnStopPlayer.Font = miniFont;
		}
	}

	private void ApplyAppColor(Color baseColor)
	{
		_appColor = baseColor;
		_currentColorHex = ColorTranslator.ToHtml(baseColor);
		Color listColor = Color.FromArgb(100, ControlPaint.Dark(baseColor));
		Color darkColor = Color.FromArgb(120, ControlPaint.Dark(baseColor));

		try { BackColor = Color.FromArgb(255, DarkenColor(baseColor, 0.82)); } catch (Exception ex) { Logger.Error("Form1.ApplyAppColor", ex); }
		timerPanel.BackColor = Color.Transparent;
		pnlTopButtons.BackColor = Color.Transparent;
		pnlTimerControls.BackColor = Color.Transparent;
		tasksHeaderPanel.BackColor = Color.Transparent;
		cassettesHeaderPanel.BackColor = Color.Transparent;
		toolsHeaderPanel.BackColor = Color.Transparent;
		playerFooterPanel.BackColor = Color.Transparent;
		playerFooterPanel.Invalidate();
		toolsRow.Invalidate();
		tasksListPanel.Invalidate();
		tasksListPanel.BackColor = Color.Transparent;
		pnlFullListRow.BackColor = Color.Transparent;
		pnlCassetteContainer.BackColor = Color.Transparent;
		pnlEqualizer.BackColor = darkColor;
		pnlVolume.BackColor = darkColor;
		pnlProgressFill.Invalidate();

		lblHours.ForeColor = Color.White;
		lblMinutes.ForeColor = Color.White;
		lblSeconds.ForeColor = Color.White;

		UpdateControlContrast(tasksHeaderPanel, Color.Transparent);
		UpdateControlContrast(cassettesHeaderPanel, Color.Transparent);
		UpdateControlContrast(toolsHeaderPanel, Color.Transparent);
		UpdateControlContrast(playerFooterPanel, Color.Transparent);

		foreach (Control c in tasksListPanel.Controls)
		{
			if (c is Panel p && p.Tag is TaskData)
			{
				p.BackColor = listColor;
				UpdateControlContrast(p, listColor);
			}
			else if (c.Name == "pnlTaskInfo")
			{
				c.BackColor = Color.Transparent;
			}
		}

		timerPanel.Invalidate();
		lblHours.Invalidate();
		lblMinutes.Invalidate();
		lblSeconds.Invalidate();
		InvalidatePresetButtons();
	}

	private void LoadThemesFile()
	{
		string imgDir = PathHelper.GetImgDir();
		string themeFile = Path.Combine(imgDir, "THEME.txt");
		if (!File.Exists(themeFile)) return;

		_themesList.Clear();
		foreach (string line in File.ReadAllLines(themeFile))
		{
			string t = line.Trim();
			if (t.StartsWith(";") || string.IsNullOrEmpty(t)) continue;
			if (t.StartsWith("[TEMA"))
			{
				int close = t.IndexOf(']');
				string parts = t.Substring(close + 1).Trim();
				string[] arr = parts.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
				if (arr.Length >= 2)
				{
					string tvPath = Path.Combine(imgDir, arr[0]);
					string thPath = Path.Combine(imgDir, arr[1]);
					if (File.Exists(tvPath) && File.Exists(thPath))
						_themesList.Add((tvPath, thPath));
				}
			}
		}
	}

	private void CycleThemeImage()
	{
		if (_themesList.Count == 0) return;
		_currentCustomThemeIndex = (_currentCustomThemeIndex + 1) % _themesList.Count;
		var theme = _themesList[_currentCustomThemeIndex];

		Image tvImg = PathHelper.LoadImage(theme.TvPath);
		SetTimerBackground(tvImg, false);
		timerPanel.Height = tvImg.Height - 4;
		timerPanel.BackColor = Color.Transparent;
		_currentTvPath = theme.TvPath;

		timerPanel.Invalidate();
		InvalidatePresetButtons();
		Invalidate();
	}

	private void UpdateTVFrame(string themePath)
	{
		try
		{
			string? path = Path.GetDirectoryName(themePath) ?? "";
			string tvName = "old_TV.gif";
			if (themePath.EndsWith("06_TH.gif", StringComparison.OrdinalIgnoreCase))
			{
				tvName = "steel_TV.gif";
			}
			else if (themePath.EndsWith("03_TH.gif", StringComparison.OrdinalIgnoreCase))
			{
				tvName = "gcard_TV.gif";
			}
			else if (themePath.EndsWith("01_TH.gif", StringComparison.OrdinalIgnoreCase))
			{
				tvName = "bluesk_TV.gif";
			}
			else if (themePath.EndsWith("04_TH.gif", StringComparison.OrdinalIgnoreCase))
			{
				tvName = "flowbk_TV.gif";
			}
			string tvPath = Path.Combine(path, tvName);
			if (File.Exists(tvPath))
			{
			Image img = PathHelper.LoadImage(tvPath);
			SetTimerBackground(img, false);
				timerPanel.Height = img.Height - 4;
			}
		}
		catch (Exception ex)
		{
			Logger.Error("Form1.UpdateTVFrame", ex);
		}
	}
	
	private void ApplyCurrentTheme()
	{
		_appColor = Color.Transparent;
		(Color, Color, Color, Color) theme = _themes[_currentThemeIndex];
		Color darkSolid = Color.FromArgb(20, 20, 20);
		try
		{
			BackColor = Color.FromArgb(255, DarkenColor(theme.Item1, 0.82));
		}
		catch (Exception ex)
		{
			Logger.Error("Form1.ApplyCurrentTheme.BackColor", ex);
		}
		pnlGrip.BackColor = Color.FromArgb(40, 40, 40);
		pnlGrip.Height = 20;
		pnlGrip.BorderStyle = BorderStyle.None;
		tasksHeaderPanel.BackColor = Color.Transparent;
		cassettesHeaderPanel.BackColor = Color.Transparent;
		toolsHeaderPanel.BackColor = Color.Transparent;
		tasksListPanel.BackColor = Color.Transparent;
		pnlFullListRow.BackColor = Color.Transparent;
		playerFooterPanel.BackColor = Color.Transparent;
		if (_currentThemeImage != null)
		{
			timerPanel.BackColor = Color.Transparent;
		}
		else
		{
			timerPanel.BackColor = theme.Item1;
		}
		UpdateControlContrast(tasksHeaderPanel, Color.Transparent);
		UpdateControlContrast(cassettesHeaderPanel, Color.Transparent);
		UpdateControlContrast(toolsHeaderPanel, Color.Transparent);
		UpdateControlContrast(playerFooterPanel, Color.Transparent);
		UpdateControlContrast(pnlGrip, pnlGrip.BackColor);
		SetSafeBackColor(pnlEqualizer, darkSolid);
		SetSafeBackColor(pnlVolume, darkSolid);
		InvalidatePresetButtons();
	}

	private void SetSafeBackColor(Control c, Color bg)
	{
		try
		{
			c.BackColor = bg;
		}
		catch (Exception ex)
		{
			Logger.Error("Form1.SetSafeBackColor", ex);
		}
	}
	
	private void UpdateControlContrast(Control parent, Color bg)
	{
		Color fg = (parent.ForeColor = Color.White);
		foreach (Control c in parent.Controls)
		{
			if (c.Name == "btnLive") continue;
			if (c.Name == "txtCassetteNum") continue;
			if (c.Name == "btnVolLow" || c.Name == "btnVolMid" || c.Name == "btnVolMax" ||
				c.Name == "btnPrevM3u" || c.Name == "btnNextM3u") continue;
			if (c is Label || c is Button || c is TextBox)
			{
				c.ForeColor = fg;
				if (c is Label && c.BackColor == Color.Transparent)
				{
				}
				else
				{
					SetSafeBackColor(c, bg);
				}
				if (c is Button b)
				{
					b.FlatAppearance.BorderColor = fg;
					b.ForeColor = fg;
				}
			}
			if (c.HasChildren)
			{
				UpdateControlContrast(c, bg);
			}
		}
	}
}
