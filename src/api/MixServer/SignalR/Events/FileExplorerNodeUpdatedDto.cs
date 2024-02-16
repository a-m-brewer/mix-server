using MixServer.Application.FileExplorer.Queries.GetNode;

namespace MixServer.SignalR.Events;

public class FileExplorerNodeUpdatedDto(FileExplorerNodeResponse node, string oldAbsolutePath)
{
    public FileExplorerNodeResponse Node { get; } = node;
    public string OldAbsolutePath { get; } = oldAbsolutePath;
}