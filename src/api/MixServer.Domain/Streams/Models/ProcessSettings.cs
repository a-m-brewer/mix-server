namespace MixServer.Domain.Streams.Models;

using Microsoft.Extensions.Logging;

public class ProcessSettings
{
    public string? WorkingDirectory { get; set; }
    public LogLevel StdOutLogLevel { get; set; } = LogLevel.Debug;
    public LogLevel StdErrLogLevel { get; set; } = LogLevel.Error;
    
    public Action? OnExit { get; set; }
}