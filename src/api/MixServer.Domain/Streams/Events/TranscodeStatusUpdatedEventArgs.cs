using MixServer.Domain.FileExplorer.Models;

namespace MixServer.Domain.Streams.Events;

public class TranscodeStatusUpdatedEventArgs : EventArgs
{
    public required NodePath Path { get; init; }
}