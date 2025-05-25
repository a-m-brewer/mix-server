using MixServer.Domain.FileExplorer.Entities;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.Sessions.Repositories;

namespace MixServer.Domain.FileExplorer.Services.Caching;

public interface IFolderPersistenceService
{
    Task<IFileExplorerFolderNodeWithEntity> GetFolderAsync(NodePath nodePath);
    Task<IFileExplorerFileNodeWithEntity> GetFileAsync(NodePath nodePath);
}

public class FolderPersistenceService(
    IFolderCacheService folderCache,
    IFolderExplorerNodeEntityRepository nodeRepository,
    IRootFileExplorerFolder rootFolder) : IFolderPersistenceService
{
    public async Task<IFileExplorerFolderNodeWithEntity> GetFolderAsync(NodePath nodePath)
    {
        var folder = folderCache.GetOrAdd(nodePath);
        
        var nodeEntity = await nodeRepository.GetOrDefaultAsync<FileExplorerFolderNodeEntity>(nodePath);
        
        if (nodeEntity is not null)
        {
            return new FileExplorerFolderNodeWithEntity(folder.Folder.Node, nodeEntity);
        }
        
        var root = await GetOrAddRootAsync(nodePath);
        
        nodeEntity = new FileExplorerFolderNodeEntity
        {
            Id = Guid.NewGuid(),
            RelativePath = folder.Folder.Node.Path.RelativePath,
            RootChild = root
        };
        await nodeRepository.AddAsync(nodeEntity);
        
        return new FileExplorerFolderNodeWithEntity(folder.Folder.Node, nodeEntity);
    }

    public async Task<IFileExplorerFileNodeWithEntity> GetFileAsync(NodePath nodePath)
    {
        var file = folderCache.GetFile(nodePath);
        
        var nodeEntity = await nodeRepository.GetOrDefaultAsync<FileExplorerFileNodeEntity>(nodePath);

        if (nodeEntity is not null)
        {
            return new FileExplorerFileNodeWithEntity(file, nodeEntity);
        }
        
        var root = await GetOrAddRootAsync(nodePath);

        nodeEntity = new FileExplorerFileNodeEntity
        {
            Id = Guid.NewGuid(),
            RelativePath = file.Path.RelativePath,
            RootChild = root
        };
        await nodeRepository.AddAsync(nodeEntity);
        
        return new FileExplorerFileNodeWithEntity(file, nodeEntity);
    }

    private async Task<FileExplorerRootChildNodeEntity> GetOrAddRootAsync(NodePath nodePath)
    {
        var rootChild = rootFolder.GetRootChildOrThrow(nodePath);

        var rootEntity = await nodeRepository.GetRootChildOrDefaultAsync(rootChild.Path);
        
        if (rootEntity is not null)
        {
            return rootEntity;
        }

        rootEntity = new FileExplorerRootChildNodeEntity
        {
            Id = Guid.NewGuid(),
            RelativePath = rootChild.Path.RootPath
        };
        await nodeRepository.AddAsync(rootEntity);
        
        return rootEntity;
    }
}