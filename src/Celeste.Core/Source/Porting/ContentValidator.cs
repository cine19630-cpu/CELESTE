using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Celeste.Porting;

public static class ContentValidator
{
    public sealed record Result(bool Ok, string Summary, List<string> Problems);

    private static readonly string[] RequiredDirs = new[]
    {
        "Dialog",
        "Effects",
        "Graphics"
    };

    public static Result Validate(IPathsProvider paths, IFileSystem fs, bool requireAudio)
    {
        var problems = new List<string>();

        if (paths == null || fs == null)
            return new Result(false, "Paths/FS não inicializados", new List<string> { "PathsProvider ou FileSystem está null." });

        var content = paths.ContentPath;

        if (!Directory.Exists(content))
        {
            problems.Add($"Pasta Content não existe: {content}");
            return new Result(false, "Content ausente", problems);
        }

        // Content vazio?
        try
        {
            if (!Directory.EnumerateFileSystemEntries(content).Any())
            {
                problems.Add($"Pasta Content está vazia: {content}");
                return new Result(false, "Content vazio", problems);
            }
        }
        catch (Exception ex)
        {
            problems.Add($"Falha ao enumerar Content: {ex.GetType().Name}: {ex.Message}");
            return new Result(false, "Não foi possível ler Content", problems);
        }

        // Pastas mínimas + detecção de case mismatch
        foreach (var dirName in RequiredDirs)
        {
            var expected = Path.Combine(content, dirName);
            if (Directory.Exists(expected))
            {
                // Conteúdo mínimo
                if (dirName == "Effects")
                {
                    var anyXnb = Directory.EnumerateFiles(expected, "*.xnb", SearchOption.AllDirectories).Any();
                    if (!anyXnb) problems.Add($"Effects existe mas não contém .xnb: {expected}");
                }
                else if (dirName == "Dialog")
                {
                    var anyFile = Directory.EnumerateFiles(expected, "*.*", SearchOption.AllDirectories).Any();
                    if (!anyFile) problems.Add($"Dialog existe mas está vazio: {expected}");
                }
                else if (dirName == "Graphics")
                {
                    var anyItem = Directory.EnumerateFileSystemEntries(expected).Any();
                    if (!anyItem) problems.Add($"Graphics existe mas está vazio: {expected}");
                }
                continue;
            }

            // Case mismatch
            var found = FindDirectoryCaseInsensitive(content, dirName);
            if (found != null)
            {
                problems.Add($"Case mismatch: esperado '{dirName}/' mas encontrado '{Path.GetFileName(found)}/'. Renomeie para exatamente '{dirName}/'.");
            }
            else
            {
                problems.Add($"Pasta crítica ausente: {dirName}/ (esperado em {expected})");
            }
        }

        if (requireAudio)
        {
            var fmodDir = Path.Combine(content, "FMOD");
            if (!Directory.Exists(fmodDir))
            {
                var found = FindDirectoryCaseInsensitive(content, "FMOD");
                if (found != null)
                    problems.Add($"Case mismatch: esperado 'FMOD/' mas encontrado '{Path.GetFileName(found)}/'. Renomeie para 'FMOD/'.");
                else
                    problems.Add("Pasta FMOD ausente (áudio pode falhar): FMOD/");
            }
        }

        var ok = problems.Count == 0;
        var summary = ok ? "OK" : "INCOMPLETO";
        return new Result(ok, summary, problems);
    }

    private static string FindDirectoryCaseInsensitive(string parent, string name)
    {
        try
        {
            foreach (var d in Directory.EnumerateDirectories(parent))
            {
                if (string.Equals(Path.GetFileName(d), name, StringComparison.OrdinalIgnoreCase))
                    return d;
            }
        }
        catch { }
        return null;
    }
}
