using MixServer.Domain.FileExplorer.Models;

namespace MixServer.Domain.FileExplorer.Services;

public interface IFileService
{
    Task<IFileExplorerFolder> GetFolderAsync(NodePath nodePath);
    Task<IFileExplorerFolder> GetFolderOrRootAsync(NodePath? nodePath);
    Task<List<IFileExplorerFileNode>> GetFilesAsync(IReadOnlyList<NodePath> nodePaths);
    Task<IFileExplorerFileNode> GetFileAsync(NodePath nodePath);
    void CopyNode(
        NodePath sourcePath,
        NodePath destinationPath,
        bool move,
        bool overwrite);
    void DeleteNode(NodePath nodePath);
    Task SetFolderSortAsync(IFolderSortRequest request, CancellationToken cancellationToken);
}