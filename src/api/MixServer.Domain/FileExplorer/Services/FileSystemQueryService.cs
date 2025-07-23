using MixServer.Domain.FileExplorer.Entities;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Repositories;
using MixServer.Domain.FileExplorer.Repositories.DbQueryOptions;

namespace MixServer.Domain.FileExplorer.Services;

public interface IFileSystemQueryService
{
    Task<IFileExplorerFolderEntity?> GetRootChildOrFolderNodeOrDefaultAsync(NodePath nodePath,
        GetFolderQueryOptions queryOptions,
        CancellationToken cancellationToken = default);
    
    Task<FileExplorerFolderNodeEntity?> GetFolderNodeOrDefaultAsync(NodePath nodePath,
        GetFolderQueryOptions queryOptions,
        CancellationToken cancellationToken = default);

    Task<List<FileExplorerNodeEntity>> GetNodesAsync(string rootPath, List<NodePath> fsChildPaths,
        GetFolderQueryOptions folderQuery, GetFileQueryOptions fileQuery, CancellationToken cancellationToken);
    
    Task<HashSet<FileExplorerNodeEntity>> GetNodesAsync(NodePath parentNodePath,
        GetFileQueryOptions fileOptions,
        GetFolderQueryOptions folderOptions,
        CancellationToken cancellationToken);
}

public class FileSystemQueryService(
    IFileSystemFolderMetadataService fileSystemFolderMetadataService,
    IFileExplorerNodeRepository fileExplorerNodeRepository) : IFileSystemQueryService
{
    public async Task<IFileExplorerFolderEntity?> GetRootChildOrFolderNodeOrDefaultAsync(NodePath nodePath, GetFolderQueryOptions queryOptions,
        CancellationToken cancellationToken = default)
    {
        if (nodePath.IsRootChild)
        {
            return await GetRootChildFolderNodeOrDefaultAsync(nodePath, queryOptions, cancellationToken);
        }

        return await GetFolderNodeOrDefaultAsync(nodePath, queryOptions, cancellationToken);
    }

    public async Task<FileExplorerFolderNodeEntity?> GetFolderNodeOrDefaultAsync(NodePath nodePath,
        GetFolderQueryOptions queryOptions,
        CancellationToken cancellationToken = default)
    {
        var metadata = await fileSystemFolderMetadataService.GetOrDefaultAsync(nodePath, cancellationToken);

        if (metadata is not null)
        {
            return await fileExplorerNodeRepository.GetFolderNodeOrDefaultAsync(metadata.FolderId, queryOptions, cancellationToken);
        }

        var node = await fileExplorerNodeRepository.GetFolderNodeOrDefaultAsync(nodePath, queryOptions, cancellationToken);

        if (node is null)
        {
            return null;
        }

        await fileSystemFolderMetadataService.CreateMetadataAsync(nodePath, node.Id, cancellationToken);
            
        return node;
    }
    
    private async Task<FileExplorerRootChildNodeEntity?> GetRootChildFolderNodeOrDefaultAsync(NodePath nodePath,
        GetFolderQueryOptions queryOptions,
        CancellationToken cancellationToken = default)
    {
        var metadata = await fileSystemFolderMetadataService.GetOrDefaultAsync(nodePath, cancellationToken);

        if (metadata is not null)
        {
            var idNode = await fileExplorerNodeRepository.GetRootChildFolderNodeOrDefaultAsync(metadata.FolderId, queryOptions, cancellationToken);
            if (idNode is not null)
            {
                return idNode;
            }
        }

        var node = await fileExplorerNodeRepository.GetRootChildFolderNodeOrDefaultAsync(nodePath, queryOptions, cancellationToken);

        if (node is null)
        {
            return null;
        }

        await fileSystemFolderMetadataService.CreateMetadataAsync(nodePath, node.Id, cancellationToken);
            
        return node;
    }

    public async Task<List<FileExplorerNodeEntity>> GetNodesAsync(string rootPath,
        List<NodePath> fsChildPaths,
        GetFolderQueryOptions folderQuery,
        GetFileQueryOptions fileQuery,
        CancellationToken cancellationToken)
    {
        var filePaths = fsChildPaths
            .Where(w => !w.IsDirectory)
            .Select(s => s.RelativePath);
        var folderIds = (await Task.WhenAll(fsChildPaths
            .Where(w => w.IsDirectory)
            .Select(s => fileSystemFolderMetadataService.GetOrDefaultAsync(s, cancellationToken))))
            .Where(w => w is not null)
            .Select(s => s!.FolderId);
        
        var folders = (await fileExplorerNodeRepository.GetFolderNodesAsync(rootPath, folderIds, folderQuery, cancellationToken))
            .Cast<FileExplorerNodeEntity>();
        var files = (await fileExplorerNodeRepository.GetFileNodesAsync(rootPath, filePaths, fileQuery, cancellationToken))
            .Cast<FileExplorerNodeEntity>();

        return folders.Concat(files).ToList();
    }

    public async Task<HashSet<FileExplorerNodeEntity>> GetNodesAsync(NodePath parentNodePath,
        GetFileQueryOptions fileOptions,
        GetFolderQueryOptions folderOptions,
        CancellationToken cancellationToken)
    {
        var folders = await fileExplorerNodeRepository.GetFolderNodesAsync(parentNodePath, folderOptions, cancellationToken);
        var files = await fileExplorerNodeRepository.GetFileNodesAsync(parentNodePath, fileOptions, cancellationToken);
        
        return folders
            .Cast<FileExplorerNodeEntity>()
            .Concat(files)
            .ToHashSet();
    }
}