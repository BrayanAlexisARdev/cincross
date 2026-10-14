using System;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Windows.Forms;

namespace CINCROSS;

public static class FontHelper
{
	private static readonly PrivateFontCollection _pfc = new PrivateFontCollection();
	private static PrivateFontCollection? _pfcDdisco;

	public static FontFamily? CustomFontFamily { get; private set; }

	public static FontFamily? DdiscoFontFamily { get; private set; }

	static FontHelper()
	{
		LoadFont();
		LoadDdisco();
	}

	private static void LoadFont()
	{
		try
		{
			string nasaPath = Path.Combine(PathHelper.GetFilesDir(), "typo", "nasa.otf");
			if (!File.Exists(nasaPath))
				nasaPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "files", "typo", "nasa.otf");

			if (string.IsNullOrEmpty(nasaPath))
			{
				return;
			}
			_pfc.AddFontFile(nasaPath);
			if (_pfc.Families.Length != 0)
			{
				CustomFontFamily = _pfc.Families[0];
			}
		}
		catch (Exception ex)
		{
			Logger.Error("FontHelper.LoadFont", ex);
		}
	}
	private static void LoadDdisco()
	{
		try
		{
			string path = Path.Combine(PathHelper.GetFilesDir(), "typo", "ddisco.ttf");
			if (!File.Exists(path))
				path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "files", "typo", "ddisco.ttf");

			if (!File.Exists(path))
			{
				return;
			}
			_pfcDdisco = new PrivateFontCollection();
			_pfcDdisco.AddFontFile(path);
			if (_pfcDdisco.Families.Length != 0)
			{
				DdiscoFontFamily = _pfcDdisco.Families[0];
			}
		}
		catch (Exception ex)
		{
			Logger.Error("FontHelper.LoadDdisco", ex);
		}
	}

	public static Font CreateDdiscoFont(float size, FontStyle style = FontStyle.Regular)
	{
		if (DdiscoFontFamily != null)
		{
			return new Font(DdiscoFontFamily, size, style);
		}
		return new Font("Segoe UI", size, style);
	}

	public static void ApplyFont(Control parent, float size = 9f, FontStyle style = FontStyle.Regular, string[]? excludeNames = null)
	{
		if (CustomFontFamily == null)
		{
			return;
		}
		bool shouldExclude = false;
		if (excludeNames != null)
		{
			foreach (string name in excludeNames)
			{
				if (parent.Name == name)
				{
					shouldExclude = true;
					break;
				}
			}
		}
		if (!shouldExclude)
		{
			parent.Font = new Font(CustomFontFamily, size, style);
		}
		foreach (Control control in parent.Controls)
		{
			ApplyFont(control, size, style, excludeNames);
		}
	}
}
