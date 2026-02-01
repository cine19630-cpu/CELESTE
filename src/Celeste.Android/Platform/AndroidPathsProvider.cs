using System;
using System.IO;
using Android.Content;

using Celeste.Porting;

namespace celestegame.app;

public sealed class AndroidPathsProvider : IPathsProvider
{
    public string BaseDataPath { get; }
    public string ContentPath { get; }
    public string LogsPath { get; }
    public string SavePath { get; }

    public AndroidPathsProvider(Context ctx)
    {
        // Prioridade: app-specific external files (não exige permissão ampla).
        var baseDir = ctx.GetExternalFilesDir(null)?.AbsolutePath;
        if (string.IsNullOrEmpty(baseDir))
            baseDir = ctx.FilesDir?.AbsolutePath ?? "/data/data/celestegame.app/files";

        BaseDataPath = baseDir;
        ContentPath = Path.Combine(BaseDataPath, "Content");
        LogsPath = Path.Combine(BaseDataPath, "Logs");
        SavePath = Path.Combine(BaseDataPath, "Save");
    }
}
