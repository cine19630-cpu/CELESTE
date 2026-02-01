using System;

namespace Celeste.Porting;

/// <summary>
/// Ponto único para serviços de plataforma (o "head" Android injeta aqui antes de instanciar o jogo).
/// Mantém o Core livre de dependências Android.
/// </summary>
public static class PortServices
{
    /// <summary>Fornece BaseDataPath/ContentPath/LogsPath/SavePath.</summary>
    public static IPathsProvider Paths { get; set; }

    /// <summary>Logger multiplataforma (Android head escreve em Logcat + arquivo).</summary>
    public static ILogger Logger { get; set; }

    /// <summary>Filesystem adapter para redirecionar Content/Save e logar falhas.</summary>
    public static IFileSystem FileSystem { get; set; }

    /// <summary>Boot manager: valida Content e controla a tela de erro/diagnóstico.</summary>
    public static IBootManager Boot { get; set; }

    public static void Info(string tag, string message)
    {
        try { Logger?.Info(tag, message); } catch { }
    }

    public static void Warn(string tag, string message)
    {
        try { Logger?.Warn(tag, message); } catch { }
    }

    public static void Error(string tag, string message)
    {
        try { Logger?.Error(tag, message); } catch { }
    }

    public static void Exception(string tag, Exception ex, string context = null)
    {
        try { Logger?.Exception(tag, ex, context); } catch { }
    }
}
