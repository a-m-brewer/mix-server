using MixServer.Application.FileExplorer.Dtos;
using MixServer.Application.FileExplorer.Queries.GetNode;

namespace MixServer.SignalR.Events;

public class FileExplorerNodeDeletedDto
{
    public required FileExplorerFolderNodeResponse Parent { get; init; }

    public required NodePathDto NodePath { get; init; }
}