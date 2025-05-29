using MixServer.Domain.FileExplorer.Entities;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.Sessions.Repositories;

namespace MixServer.Domain.FileExplorer.Services.Caching;

public interface IFolderPersistenceService
{
    Task<IFileExplorerFolderNodeWithEntity> GetFolderAsync(NodePath nodePath, CancellationToken cancellationToken = default);
    Task<IFileExplorerFileNodeWithEntity> GetFileAsync(NodePath nodePath, CancellationToken cancellationToken = default);
}

public class FolderPersistenceService(
    IFolderCacheService folderCache,
    IFolderExplorerNodeEntityRepository nodeRepository,
    IRootFileExplorerFolder rootFolder) : IFolderPersistenceService
{
    public async Task<IFileExplorerFolderNodeWithEntity> GetFolderAsync(NodePath nodePath, CancellationToken cancellationToken = default)
    {
        var folder = await folderCache.GetOrAddAsync(nodePath);
        
        var nodeEntity = await nodeRepository.GetOrDefaultAsync<FileExplorerFolderNodeEntity>(nodePath, cancellationToken);
        
        if (nodeEntity is not null)
        {
            return new FileExplorerFolderNodeWithEntity(folder.Folder.Node, nodeEntity);
        }
        
        var root = await GetOrAddRootAsync(nodePath, cancellationToken);
        
        nodeEntity = new FileExplorerFolderNodeEntity
        {
            Id = Guid.NewGuid(),
            RelativePath = folder.Folder.Node.Path.RelativePath,
            RootChild = root
        };
        await nodeRepository.AddAsync(nodeEntity, cancellationToken);
        
        return new FileExplorerFolderNodeWithEntity(folder.Folder.Node, nodeEntity);
    }

    public async Task<IFileExplorerFileNodeWithEntity> GetFileAsync(NodePath nodePath, CancellationToken cancellationToken = default)
    {
        var file = await folderCache.GetFileAsync(nodePath);
        
        var nodeEntity = await nodeRepository.GetOrDefaultAsync<FileExplorerFileNodeEntity>(nodePath, cancellationToken);

        if (nodeEntity is not null)
        {
            return new FileExplorerFileNodeWithEntity(file, nodeEntity);
        }
        
        var root = await GetOrAddRootAsync(nodePath, cancellationToken);

        nodeEntity = new FileExplorerFileNodeEntity
        {
            Id = Guid.NewGuid(),
            RelativePath = file.Path.RelativePath,
            RootChild = root
        };
        await nodeRepository.AddAsync(nodeEntity, cancellationToken);
        
        return new FileExplorerFileNodeWithEntity(file, nodeEntity);
    }

    private async Task<FileExplorerRootChildNodeEntity> GetOrAddRootAsync(NodePath nodePath, CancellationToken cancellationToken = default)
    {
        var rootChild = rootFolder.GetRootChildOrThrow(nodePath);

        var rootEntity = await nodeRepository.GetRootChildOrDefaultAsync(rootChild.Path, cancellationToken);
        
        if (rootEntity is not null)
        {
            return rootEntity;
        }

        rootEntity = new FileExplorerRootChildNodeEntity
        {
            Id = Guid.NewGuid(),
            RelativePath = rootChild.Path.RootPath
        };
        await nodeRepository.AddAsync(rootEntity, cancellationToken);
        
        return rootEntity;
    }
}