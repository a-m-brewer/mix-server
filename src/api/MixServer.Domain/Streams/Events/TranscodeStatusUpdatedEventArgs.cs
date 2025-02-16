namespace MixServer.Domain.Streams.Events;

public class TranscodeStatusUpdatedEventArgs : EventArgs
{
    public required string AbsoluteFilePath { get; init; }
}