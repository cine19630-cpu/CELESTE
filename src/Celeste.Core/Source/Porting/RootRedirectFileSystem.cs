using System;
using System.Collections.Generic;
using System.IO;

namespace Celeste.Porting;

/// <summary>
/// Implementação padrão (System.IO) com redirecionamento de raízes para manter paths originais do PC.
/// - Qualquer path relativo que comece com "Content" é redirecionado para Paths.ContentPath.
/// - Qualquer path relativo que comece com "Save" (ou "Saves") é redirecionado para Paths.SavePath.
/// Não tenta resolver paths absolutos: se for rooted, usa como está.
/// </summary>
public sealed class RootRedirectFileSystem : IFileSystem
{
    private readonly IPathsProvider paths;
    private readonly ILogger log;

    public RootRedirectFileSystem(IPathsProvider paths, ILogger logger)
    {
        this.paths = paths;
        this.log = logger;
    }

    private string Normalize(string p)
    {
        if (string.IsNullOrEmpty(p))
            return p;

        // Já é absoluto
        if (Path.IsPathRooted(p))
            return p;

        var norm = p.Replace('\\', '/');

        // Content/
        if (norm.StartsWith("Content/", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(norm, "Content", StringComparison.OrdinalIgnoreCase))
        {
            var rel = norm.Length <= 7 ? "" : norm.Substring(8); // remove "Content/"
            return Path.Combine(paths.ContentPath, rel.Replace('/', Path.DirectorySeparatorChar));
        }

        // Save/ ou Saves/
        if (norm.StartsWith("Save/", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(norm, "Save", StringComparison.OrdinalIgnoreCase) ||
            norm.StartsWith("Saves/", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(norm, "Saves", StringComparison.OrdinalIgnoreCase))
        {
            var rel = norm;
            if (rel.StartsWith("Save/", StringComparison.OrdinalIgnoreCase)) rel = rel.Substring(5);
            else if (string.Equals(rel, "Save", StringComparison.OrdinalIgnoreCase)) rel = "";
            else if (rel.StartsWith("Saves/", StringComparison.OrdinalIgnoreCase)) rel = rel.Substring(6);
            else if (string.Equals(rel, "Saves", StringComparison.OrdinalIgnoreCase)) rel = "";
            return Path.Combine(paths.SavePath, rel.Replace('/', Path.DirectorySeparatorChar));
        }

        // Default: relativo ao BaseDataPath (evita depender de current directory)
        return Path.Combine(paths.BaseDataPath, norm.Replace('/', Path.DirectorySeparatorChar));
    }

    public bool DirectoryExists(string path)
    {
        var p = Normalize(path);
        return Directory.Exists(p);
    }

    public bool FileExists(string path)
    {
        var p = Normalize(path);
        return File.Exists(p);
    }

    public IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption option)
    {
        var p = Normalize(path);
        return Directory.EnumerateFiles(p, searchPattern, option);
    }

    public IEnumerable<string> EnumerateDirectories(string path, string searchPattern, SearchOption option)
    {
        var p = Normalize(path);
        return Directory.EnumerateDirectories(p, searchPattern, option);
    }

    public Stream OpenRead(string path)
    {
        var p = Normalize(path);
        return File.OpenRead(p);
    }

    public Stream OpenWrite(string path, bool overwrite)
    {
        var p = Normalize(path);
        var dir = Path.GetDirectoryName(p);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        return new FileStream(p, overwrite ? FileMode.Create : FileMode.CreateNew, FileAccess.Write, FileShare.Read);
    }

    public void CreateDirectory(string path)
    {
        var p = Normalize(path);
        Directory.CreateDirectory(p);
    }

    public void Move(string src, string dst, bool overwrite)
    {
        var s = Normalize(src);
        var d = Normalize(dst);
        var dir = Path.GetDirectoryName(d);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        if (overwrite && File.Exists(d))
            File.Delete(d);

        File.Move(s, d);
    }

    public string ReadAllText(string path)
    {
        var p = Normalize(path);
        return File.ReadAllText(p);
    }

    public void WriteAllText(string path, string text)
    {
        var p = Normalize(path);
        var dir = Path.GetDirectoryName(p);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllText(p, text);
    }
}
