using MixServer.Domain.FileExplorer.Entities;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.Sessions.Repositories;

namespace MixServer.Domain.FileExplorer.Services;

public interface IFileSystemQueryService
{
    Task<FileExplorerFolderNodeEntity?> GetFolderNodeOrDefaultAsync(
        NodePath nodePath,
        bool includeChildren = true,
        CancellationToken cancellationToken = default);
}

public class FileSystemQueryService(
    IFileSystemFolderMetadataService fileSystemFolderMetadataService,
    IFolderExplorerNodeEntityRepository folderExplorerNodeEntityRepository) : IFileSystemQueryService
{
    public async Task<FileExplorerFolderNodeEntity?> GetFolderNodeOrDefaultAsync(
        NodePath nodePath,
        bool includeChildren = true,
        CancellationToken cancellationToken = default)
    {
        var metadata = await fileSystemFolderMetadataService.GetOrDefaultAsync(nodePath, cancellationToken);

        if (metadata is not null)
        {
            return await folderExplorerNodeEntityRepository.GetFolderNodeOrDefaultAsync(metadata.FolderId, includeChildren, cancellationToken);
        }

        var node = await folderExplorerNodeEntityRepository.GetFolderNodeOrDefaultAsync(nodePath, includeChildren, cancellationToken);

        if (node is null)
        {
            return null;
        }

        await fileSystemFolderMetadataService.CreateMetadataAsync(nodePath, node.Id, cancellationToken);
            
        return node;

    }
}