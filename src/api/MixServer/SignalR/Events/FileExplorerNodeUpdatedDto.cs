using MixServer.Application.FileExplorer.Queries.GetNode;

namespace MixServer.SignalR.Events;

public class FileExplorerNodeUpdatedDto(FileExplorerNodeResponse node, string oldAbsolutePath, int index)
{
    public FileExplorerNodeResponse Node { get; } = node;
    public int Index { get; } = index;
    public string OldAbsolutePath { get; } = oldAbsolutePath;
}