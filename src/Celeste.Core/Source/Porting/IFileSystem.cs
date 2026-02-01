using System;
using System.Collections.Generic;
using System.IO;

namespace Celeste.Porting;

/// <summary>
/// Adapter de filesystem para manter o código do jogo o mais intacto possível.
/// Permite redirecionar raízes (Content/ e Save/) e instrumentar logs de falhas.
/// </summary>
public interface IFileSystem
{
    bool DirectoryExists(string path);
    bool FileExists(string path);
    IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption option);
    IEnumerable<string> EnumerateDirectories(string path, string searchPattern, SearchOption option);

    Stream OpenRead(string path);
    Stream OpenWrite(string path, bool overwrite);
    void CreateDirectory(string path);
    void Move(string src, string dst, bool overwrite);

    string ReadAllText(string path);
    void WriteAllText(string path, string text);
}
