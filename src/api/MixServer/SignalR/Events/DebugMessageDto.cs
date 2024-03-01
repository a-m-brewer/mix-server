namespace MixServer.SignalR.Events;

public class DebugMessageDto
{
    public LogLevel Level { get; set; }

    public string Message { get; set; } = string.Empty;
}