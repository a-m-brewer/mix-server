using MixServer.Application.FileExplorer.Queries.GetNode;

namespace MixServer.SignalR.Events;

public class FileExplorerNodeDeletedDto(FolderNodeResponse parent, string absolutePath)
{
    public FolderNodeResponse Parent { get; } = parent;

    public string AbsolutePath { get; set; } = absolutePath;
}