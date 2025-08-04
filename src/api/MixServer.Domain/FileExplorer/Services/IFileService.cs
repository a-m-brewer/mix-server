using MixServer.Domain.FileExplorer.Models;

namespace MixServer.Domain.FileExplorer.Services;

public interface IFileService
{
    Task<IFileExplorerFolderPage> GetFolderPageAsync(NodePath nodePath,
        Page page,
        CancellationToken cancellationToken = default);
    
    Task<IFileExplorerFolderPage> GetFolderOrRootPageAsync(NodePath? nodePath,
        Page page,
        CancellationToken cancellationToken = default);
    
    Task<IFileExplorerFolder> GetFolderAsync(NodePath nodePath,
        Page page,
        CancellationToken cancellationToken = default);
    Task<IFileExplorerFolder> GetFolderOrRootAsync(NodePath? nodePath,
        Page page,
        CancellationToken cancellationToken = default);
    Task<List<IFileExplorerFileNode>> GetFilesAsync(IReadOnlyList<NodePath> nodePaths);
    Task<IFileExplorerFileNode> GetFileAsync(NodePath nodePath);
    void CopyNode(
        NodePath sourcePath,
        NodePath destinationPath,
        bool move,
        bool overwrite);
    void DeleteNode(NodePath nodePath);
    Task<IFileExplorerFolderPage> SetFolderSortAsync(IFolderSortRequest request,
        CancellationToken cancellationToken);
}