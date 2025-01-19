using MixServer.Application.FileExplorer.Queries.GetNode;

namespace MixServer.SignalR.Events;

public class FileExplorerNodeAddedDto
{
    public required FileExplorerNodeResponse Node { get; init; }
    public required int Index { get; init; }
}