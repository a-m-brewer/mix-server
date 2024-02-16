using MixServer.Application.FileExplorer.Queries.GetNode;

namespace MixServer.SignalR.Events;

public class FileExplorerNodeDeletedDto(FileExplorerFolderNodeResponse parent, string absolutePath)
{
    public FileExplorerFolderNodeResponse Parent { get; } = parent;

    public string AbsolutePath { get; set; } = absolutePath;
}