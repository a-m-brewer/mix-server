using MixServer.Application.FileExplorer.Queries.GetNode;

namespace MixServer.SignalR.Events;

public class FileExplorerNodeUpdatedDto(NodeResponse node, string oldAbsolutePath)
{
    public NodeResponse Node { get; } = node;
    public string OldAbsolutePath { get; } = oldAbsolutePath;
}