using System;
using System.Diagnostics;
using System.IO;

namespace CINCROSS;

internal static class Logger
{
    private static readonly string _logPath = Path.Combine(
        Path.GetTempPath(), "x5ver_debug.log");

    [Conditional("DEBUG")]
    public static void Debug(string message)
    {
        var line = $"{DateTime.Now:HH:mm:ss.fff} [DEBUG] {message}";
        DebugWrite(line);
    }

    public static void Error(string context, Exception ex)
    {
        var line = $"{DateTime.Now:HH:mm:ss.fff} [ERROR] {context}: {ex.GetType().Name}: {ex.Message}";
        DebugWrite(line);
        try { File.AppendAllText(_logPath, line + Environment.NewLine); }
        catch { }
    }

    public static void Warn(string message)
    {
        var line = $"{DateTime.Now:HH:mm:ss.fff} [WARN] {message}";
        DebugWrite(line);
    }

    public static void Info(string message)
    {
        var line = $"{DateTime.Now:HH:mm:ss.fff} [INFO] {message}";
        DebugWrite(line);
    }

    [Conditional("DEBUG")]
    private static void DebugWrite(string line)
    {
        System.Diagnostics.Debug.WriteLine(line);
    }
}
