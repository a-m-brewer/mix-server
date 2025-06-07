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

    Task<List<FileExplorerNodeEntity>> GetNodesAsync(string rootPath, List<NodePath> fsChildPaths, CancellationToken cancellationToken);
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

    public async Task<List<FileExplorerNodeEntity>> GetNodesAsync(string rootPath, List<NodePath> fsChildPaths, CancellationToken cancellationToken)
    {
        var filePaths = fsChildPaths
            .Where(w => !w.IsDirectory)
            .Select(s => s.RelativePath);
        var folderIds = (await Task.WhenAll(fsChildPaths
            .Where(w => w.IsDirectory)
            .Select(s => fileSystemFolderMetadataService.GetOrDefaultAsync(s, cancellationToken))))
            .Where(w => w is not null)
            .Select(s => s!.FolderId);
        
        var folders = (await folderExplorerNodeEntityRepository.GetFolderNodesAsync(rootPath, folderIds, cancellationToken))
            .Cast<FileExplorerNodeEntity>();
        var files = (await folderExplorerNodeEntityRepository.GetFileNodesAsync(rootPath, filePaths, cancellationToken))
            .Cast<FileExplorerNodeEntity>();

        return folders.Concat(files).ToList();
    }
}