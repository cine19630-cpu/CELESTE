using System;
using System.IO;
using System.Text;

using Celeste.Porting;

namespace celestegame.app;

public sealed class LoggerTextWriter : TextWriter
{
    private readonly ILogger log;
    private readonly string tag;
    private readonly bool isError;

    public override Encoding Encoding => Encoding.UTF8;

    public LoggerTextWriter(ILogger logger, string tag, bool isError)
    {
        this.log = logger;
        this.tag = tag;
        this.isError = isError;
    }

    public override void WriteLine(string value)
    {
        if (log == null) return;
        if (isError) log.Error(tag, value);
        else log.Info(tag, value);
    }

    public override void Write(string value)
    {
        // ignora writes sem newline para não spammar
    }
}
