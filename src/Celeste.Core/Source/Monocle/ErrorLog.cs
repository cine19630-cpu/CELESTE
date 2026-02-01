using System;
using System.IO;
using System.Text;
using Celeste.Porting;

namespace Monocle;

public static class ErrorLog
{
    private static string filepath;
    public static int Counter { get; private set; }

    private static string FilePath
    {
        get
        {
            if (!string.IsNullOrEmpty(filepath))
                return filepath;

            try
            {
                var lp = PortServices.Paths?.LogsPath;
                if (!string.IsNullOrEmpty(lp))
                    filepath = Path.Combine(lp, "error_log.txt");
                else
                    filepath = "error_log.txt";
            }
            catch
            {
                filepath = "error_log.txt";
            }

            return filepath;
        }
    }

    public static void Write(Exception e)
    {
        try
        {
            Counter++;
            var sb = new StringBuilder();
            sb.AppendLine("==========================================");
            sb.AppendLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            sb.AppendLine(e.GetType().FullName + ": " + e.Message);
            sb.AppendLine(e.StackTrace);

            if (e.InnerException != null)
            {
                sb.AppendLine("---- INNER ----");
                sb.AppendLine(e.InnerException.GetType().FullName + ": " + e.InnerException.Message);
                sb.AppendLine(e.InnerException.StackTrace);
            }

            sb.AppendLine("==========================================");

            Directory.CreateDirectory(Path.GetDirectoryName(FilePath) ?? ".");
            File.AppendAllText(FilePath, sb.ToString());
            PortServices.Exception("CELESTE/ERRORLOG", e, "ErrorLog.Write");
        }
        catch { }
    }

    public static void Clear()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath) ?? ".");
            File.WriteAllText(FilePath, "");
        }
        catch { }
    }

    public static void Open()
    {
        // Android: não há Process.Start; deixe o usuário abrir via file manager/ADB.
        PortServices.Info("CELESTE/ERRORLOG", "Open() chamado (no-op no Android)");
    }
}
