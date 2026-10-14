using System;
using System.Drawing;
using System.IO;

namespace CINCROSS;

internal static class PathHelper
{
    private const int MaxParentTries = 5;

    private static readonly Lazy<string> _imgDir = new(() => ProbeDir("img"));
    private static readonly Lazy<string> _m3uDir = new(() => ProbeDir("m3u"));
    private static readonly Lazy<string> _filesDir = new(() => ProbeDir(null));

    public static string StartupPath => AppDomain.CurrentDomain.BaseDirectory;

    public static string GetImgDir() => _imgDir.Value;

    public static string GetM3uDir() => _m3uDir.Value;

    public static string GetFilesDir() => _filesDir.Value;

    private static string ProbeDir(string? sub)
    {
        string relative = sub == null ? "files" : Path.Combine("files", sub);
        string dir = Path.Combine(StartupPath, relative);
        if (Directory.Exists(dir)) return dir;

        string probe = StartupPath;
        for (int i = 0; i < MaxParentTries; i++)
        {
            dir = Path.Combine(probe, relative);
            if (Directory.Exists(dir)) return dir;
            var parent = Directory.GetParent(probe);
            if (parent == null) break;
            probe = parent.FullName;
        }
        return Path.Combine(StartupPath, relative);
    }

    public static string ResolveImg(string fileName)
    {
        return Path.Combine(GetImgDir(), fileName);
    }

    /// <summary>Loads an image. For static images (PNG), copies to memory so the file isn't locked.
    /// For GIFs, loads directly to preserve animation (file may remain open).</summary>
    public static Image LoadImage(string path)
    {
        if (path.EndsWith(".gif", StringComparison.OrdinalIgnoreCase))
            return Image.FromFile(path);
        using var img = Image.FromFile(path);
        return new Bitmap(img);
    }
}
