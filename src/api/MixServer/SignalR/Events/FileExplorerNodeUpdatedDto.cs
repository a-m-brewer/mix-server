using MixServer.Application.FileExplorer.Queries.GetNode;

namespace MixServer.SignalR.Events;

public class FileExplorerNodeUpdatedDto
{
    public required FileExplorerNodeResponse Node { get; init; }
    public required int Index { get; init; }
    public required string? OldAbsolutePath { get; init; }
}