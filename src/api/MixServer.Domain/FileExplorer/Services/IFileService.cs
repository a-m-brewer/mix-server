using MixServer.Domain.FileExplorer.Models;
using Range = MixServer.Domain.FileExplorer.Models.Range;

namespace MixServer.Domain.FileExplorer.Services;

public interface IFileService
{
    Task<IFileExplorerFolderRange> GetFolderOrRootRangeAsync(NodePath? nodePath,
        Range range,
        CancellationToken cancellationToken = default);

    void CopyNode(
        NodePath sourcePath,
        NodePath destinationPath,
        bool move,
        bool overwrite);
    void DeleteNode(NodePath nodePath);
    Task SetFolderSortAsync(IFolderSortRequest request,
        CancellationToken cancellationToken);
}