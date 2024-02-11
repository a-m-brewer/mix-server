namespace MixServer.Application.Queueing.Commands.AddToQueue;

public class AddToQueueCommand
{
    public string AbsoluteFolderPath { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;
}