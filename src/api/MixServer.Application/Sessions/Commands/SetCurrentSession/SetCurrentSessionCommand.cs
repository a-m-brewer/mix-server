namespace MixServer.Application.Sessions.Commands.SetCurrentSession;

public class SetCurrentSessionCommand
{
    public string AbsoluteFolderPath { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;
}