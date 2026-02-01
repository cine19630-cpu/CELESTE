using System;

namespace Celeste.Porting;

public interface ILogger
{
    void Info(string tag, string message);
    void Warn(string tag, string message);
    void Error(string tag, string message);
    void Exception(string tag, Exception ex, string context = null);
}
