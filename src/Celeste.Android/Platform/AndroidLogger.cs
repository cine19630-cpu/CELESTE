using System;
using System.IO;
using System.Text;
using Android.Util;

using Celeste.Porting;

namespace celestegame.app;

public sealed class AndroidLogger : ILogger, IDisposable
{
    private readonly object gate = new();
    private readonly string filePath;
    private StreamWriter writer;

    public AndroidLogger(IPathsProvider paths)
    {
        Directory.CreateDirectory(paths.LogsPath);
        var ts = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        filePath = Path.Combine(paths.LogsPath, $"log_{ts}.txt");

        writer = new StreamWriter(new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read), new UTF8Encoding(false))
        {
            AutoFlush = true
        };

        Info("CELESTE/LOG", $"LOG_FILE={filePath}");
    }

    public void Info(string tag, string message) => Write("INFO", tag, message, isError: false);
    public void Warn(string tag, string message) => Write("WARN", tag, message, isError: false);
    public void Error(string tag, string message) => Write("ERROR", tag, message, isError: true);

    public void Exception(string tag, Exception ex, string context = null)
    {
        if (ex == null)
        {
            Error(tag, context ?? "Exception(null)");
            return;
        }

        var msg = (context != null ? context + " | " : "") + ex.GetType().Name + ": " + ex.Message + "
" + ex.StackTrace;
        Write("EX", tag, msg, isError: true);
    }

    private void Write(string level, string tag, string message, bool isError)
    {
        var line = $"{DateTime.Now:HH:mm:ss.fff} | {level} | {tag} | {message}";
        lock (gate)
        {
            try
            {
                writer?.WriteLine(line);
            }
            catch { }
        }

        // Logcat (evita linhas enormes)
        if (message != null && message.Length > 3500)
            message = message.Substring(0, 3500) + "...(truncated)";

        if (isError) Log.Error(tag, message);
        else if (level == "WARN") Log.Warn(tag, message);
        else Log.Info(tag, message);
    }

    public void Flush()
    {
        lock (gate)
        {
            try { writer?.Flush(); } catch { }
        }
    }

    public void Dispose()
    {
        lock (gate)
        {
            try { writer?.Dispose(); } catch { }
            writer = null;
        }
    }
}
