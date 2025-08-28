using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.Streams.Enums;

namespace MixServer.Domain.Streams.Events;

public class TranscodeStatusRemovedEventArgs : EventArgs
{
    public required NodePath Path { get; init; }
}