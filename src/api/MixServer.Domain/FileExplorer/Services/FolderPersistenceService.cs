using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using MixServer.Domain.Extensions;
using MixServer.Domain.FileExplorer.Converters;
using MixServer.Domain.FileExplorer.Entities;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Repositories;
using MixServer.Domain.FileExplorer.Repositories.DbQueryOptions;
using MixServer.Domain.Persistence;

namespace MixServer.Domain.FileExplorer.Services;

public class ChildEntityMap
{
    public required NodePath Path { get; init; } 
    public required FileSystemInfo FsInfo { get; init; }
    public required IFileExplorerNodeEntityBase? DbEntity { get; set; }
    
    public bool IsParent { get; init; }
}

public interface IFolderPersistenceService
{
    Task<IFileExplorerFolderEntity> GetOrAddFolderAsync(NodePath nodePath, CancellationToken cancellationToken = default);
    Task<FileExplorerFileNodeEntity> GetOrAddFileAsync(NodePath nodePath, CancellationToken cancellationToken = default);
    Task<List<FileExplorerFileNodeEntity>>  GetOrAddFileRangeAsync(IReadOnlyList<NodePath> nodePaths, CancellationToken cancellationToken = default);
    IAsyncEnumerable<ChildEntityMap> AddOrUpdateFolderAsync(NodePath directoryPath,
        DirectoryInfo directory,
        CancellationToken cancellationToken = default);

    Task<FileExplorerFolderNodeEntity?> EnsureParentUpdatedAsync(
        NodePath directoryPath,
        DirectoryInfo directory,
        CancellationToken cancellationToken);

    Task<List<DirectoryInfo>> EnsureChildrenUpdatedAsync(
        NodePath parentNodePath,
        FileSystemInfo[] children,
        FsHashBuilder hashBuilder,
        CancellationToken cancellationToken);
    
    Task UpdateHashAsync(NodePath nodePath, string hash, CancellationToken cancellationToken);
}

public class FolderPersistenceService(
    IFileSystemInfoToEntityConverter fileSystemInfoToEntityConverter,
    IFileExplorerNodeRepository fileExplorerNodeRepository,
    IFileSystemQueryService fileSystemQueryService,
    ILogger<FolderPersistenceService> logger,
    IRemoveMediaMetadataChannel removeMediaMetadataChannel,
    IRootFileExplorerFolder rootFolder,
    IUpdateMediaMetadataChannel updateMediaMetadataChannel,
    IUnitOfWork unitOfWork) : IFolderPersistenceService
{

    public async Task<IFileExplorerFolderEntity> GetOrAddFolderAsync(NodePath nodePath, CancellationToken cancellationToken = default)
    {
        var existingFolder = await fileSystemQueryService.GetRootChildOrFolderNodeOrDefaultAsync(nodePath, GetFolderQueryOptions.Full, cancellationToken);

        if (existingFolder is not null && !string.IsNullOrWhiteSpace(existingFolder.Hash))
        {
            return existingFolder;
        }
        
        var directory = new DirectoryInfo(nodePath.AbsolutePath);

        IFileExplorerFolderEntity folderEntity = null!;
        await foreach (var map in AddOrUpdateFolderAsync(nodePath, directory, cancellationToken))
        {
            if (map is { IsParent: true, DbEntity: IFileExplorerFolderEntity f })
            {
                folderEntity = f;
            }
        }

        return folderEntity;
    }

    public async Task<FileExplorerFileNodeEntity> GetOrAddFileAsync(NodePath nodePath, CancellationToken cancellationToken = default)
    {
        var fileEntity = await fileExplorerNodeRepository.GetFileNodeOrDefaultAsync(nodePath, GetFileQueryOptions.MetadataOnly, cancellationToken);
        
        if (fileEntity is not null) 
        {
            return fileEntity;
        }
        
        var fileInfo = new FileInfo(nodePath.AbsolutePath);
        
        var root = await fileExplorerNodeRepository.GetRootChildOrThrowAsync(nodePath.RootPath, cancellationToken);
        var parentEntity = nodePath.Parent.IsRootChild
            ? null
            : await fileSystemQueryService.GetFolderNodeOrDefaultAsync(nodePath.Parent, GetFolderQueryOptions.FolderOnly, cancellationToken: cancellationToken);

        fileEntity = fileSystemInfoToEntityConverter.CreateFileEntity(fileInfo, root, parentEntity);
        await fileExplorerNodeRepository.AddAsync(fileEntity, cancellationToken);

        return fileEntity;
    }

    public async Task<List<FileExplorerFileNodeEntity>> GetOrAddFileRangeAsync(IReadOnlyList<NodePath> nodePaths, CancellationToken cancellationToken = default)
    {
        var fileEntities = await fileExplorerNodeRepository.GetFileNodesAsync(nodePaths, GetFileQueryOptions.MetadataOnly, cancellationToken);
        if (fileEntities.Count == nodePaths.Count)
        {
            return fileEntities.ToList();
        }
        
        var roots = await fileExplorerNodeRepository.GetRootChildrenAsync(nodePaths.Select(s => s.RootPath).Distinct(), cancellationToken);
        var parents = await fileExplorerNodeRepository.GetFolderNodesAsync(
            nodePaths.Select(s => s.Parent).Where(w => !w.IsRootChild).DistinctBy(d => d.AbsolutePath), 
            cancellationToken);

        var files = new List<FileExplorerFileNodeEntity>();
        var newEntities = new List<FileExplorerNodeEntity>();
        
        foreach (var nodePath in nodePaths)
        {
            var fileEntity = fileEntities.FirstOrDefault(f => f.RelativePath == nodePath.RelativePath && f.RootChild.RelativePath == nodePath.RootPath);
            if (fileEntity is not null)
            {
                files.Add(fileEntity);
                continue;
            }
            
            var parentPath = nodePath.Parent;
            var root = roots.First(f => f.RelativePath == nodePath.RootPath);
            var parent = parents.FirstOrDefault(f => f.RootChild.RelativePath == parentPath.RootPath && f.RelativePath == parentPath.RelativePath);
            
            var newEntity = fileSystemInfoToEntityConverter.CreateFileEntity(new  FileInfo(nodePath.AbsolutePath), root, parent);
            files.Add(newEntity);
            newEntities.Add(newEntity);
        }
        
        await fileExplorerNodeRepository.AddRangeAsync(newEntities, cancellationToken);

        return files;
    }

    public async IAsyncEnumerable<ChildEntityMap> AddOrUpdateFolderAsync(
        NodePath directoryPath,
        DirectoryInfo directory,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Received persist folder request for {Directory}", directory.FullName);
        
        var root = await fileExplorerNodeRepository.GetRootChildOrThrowAsync(directoryPath.RootPath, cancellationToken);

        using var hashBuilder = new FsHashBuilder();
        
        var parentEntity = await EnsureParentUpdatedAsync(
            directoryPath,
            directory,
            root,
            cancellationToken);

        await foreach (var map in EnsureChildrenUpdatedAsync(
                     directoryPath,
                     parentEntity,
                     directory.MsEnumerateFileSystemInfos(),
                     root,
                     hashBuilder,
                     cancellationToken))
        {
            yield return map;
        }
        
        IFileExplorerFolderEntity parentEntityOrRoot = parentEntity is null ? root : parentEntity;
        var parentInfoOrRoot = parentEntity is null ? directory : new DirectoryInfo(root.Path.AbsolutePath);

        parentEntityOrRoot.Hash = hashBuilder.ComputeHash();

        yield return new ChildEntityMap
        {
            Path = directoryPath,
            FsInfo = parentInfoOrRoot,
            DbEntity = parentEntityOrRoot,
            IsParent = true
        };
        
        logger.LogInformation("Persisted folder {Directory} ({Hash})", directory.FullName, parentEntityOrRoot.Hash);

        // unitOfWork.OnSaved(() => NotifyChanges(fsChildren, removedChildren));
    }

    public async Task<FileExplorerFolderNodeEntity?> EnsureParentUpdatedAsync(NodePath directoryPath, DirectoryInfo directory, CancellationToken cancellationToken)
    {
        var root = await fileExplorerNodeRepository.GetRootChildOrThrowAsync(directoryPath.RootPath, cancellationToken);
        
        var parentEntity = await EnsureParentUpdatedAsync(
            directoryPath,
            directory,
            root,
            cancellationToken);
        
        return parentEntity;
    }

    private async Task<FileExplorerFolderNodeEntity?> EnsureParentUpdatedAsync(
        NodePath directoryPath,
        DirectoryInfo directory,
        FileExplorerRootChildNodeEntity root,
        CancellationToken cancellationToken)
    {
        if (directoryPath.IsRootChild)
        {
            root.Update(directoryPath, directory);
            logger.LogTrace("Directory path {DirectoryPath} is root child, updated root entity", directoryPath.AbsolutePath);
            return null;
        }
        
        var grandParentPath = directoryPath.Parent;
        var grandParentEntity = grandParentPath.IsRootChild
            ? null
            : await fileSystemQueryService.GetFolderNodeOrDefaultAsync(grandParentPath, GetFolderQueryOptions.FolderOnly, cancellationToken);
            
        var parentEntity = await fileSystemQueryService.GetFolderNodeOrDefaultAsync(directoryPath, GetFolderQueryOptions.FolderOnly, cancellationToken: cancellationToken);
        if (parentEntity is null)
        {
            parentEntity = await fileSystemInfoToEntityConverter.CreateFolderEntityAsync(
                directory,
                root,
                grandParentEntity,
                cancellationToken);
            await fileExplorerNodeRepository.AddAsync(parentEntity, cancellationToken);
            logger.LogTrace("Directory path {DirectoryPath} is not in database, created new folder entity", directoryPath.AbsolutePath);
        }
        else
        {
            parentEntity.Update(directoryPath, directory, grandParentEntity);
            logger.LogTrace("Directory path {DirectoryPath} is in database, updated existing folder entity", directoryPath.AbsolutePath);
        }

        logger.LogTrace("Ensured parent entity exists and is up to date for {DirectoryPath} with ID {ParentEntityId}", directoryPath.AbsolutePath, parentEntity.Id);
        return parentEntity;
    }

    private async IAsyncEnumerable<ChildEntityMap> EnsureChildrenUpdatedAsync(
        NodePath parentNodePath,
        FileExplorerFolderNodeEntity? parentEntity,
        IEnumerable<FileSystemInfo> children,
        FileExplorerRootChildNodeEntity root,
        FsHashBuilder hashBuilder,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var dbNodes = await fileSystemQueryService.GetNodesAsync(parentNodePath, GetFileQueryOptions.MetadataOnly, GetFolderQueryOptions.FolderOnly, cancellationToken);
        
        foreach (var chunk in GetChildEntitiesAsync(dbNodes, children).Chunk(100))
        {
            var addedChildren = new List<FileExplorerNodeEntity>();
            foreach (var fsChild in chunk)
            {
                if (fsChild.DbEntity is FileExplorerNodeEntity node)
                {
                    node.Update(fsChild.Path, fsChild.FsInfo, parentEntity);
                    logger.LogDebug("Updated existing child entity for {ChildPath} with ID {ChildEntityId}", fsChild.Path.RelativePath, fsChild.DbEntity.Id);
                }
                else 
                {
                    var entity = await fileSystemInfoToEntityConverter.CreateNodeAsync(fsChild.FsInfo, root, parentEntity, cancellationToken);
                    fsChild.DbEntity = entity;
                    addedChildren.Add(entity);
                    logger.LogDebug("Created new child entity for {ChildPath} with ID {ChildEntityId}", fsChild.Path.RelativePath, fsChild.DbEntity.Id);
                }

                hashBuilder.Add(fsChild.FsInfo);
                yield return fsChild;
            }

            await fileExplorerNodeRepository.AddRangeAsync(addedChildren, cancellationToken);
        }

        // If there are any nodes in the database that were not found in the file system, remove them.
        // Nodes are removed in GetChildEntitiesAsync, so we can just check the remaining dbNodes.
        if (dbNodes.Count > 0)
        {
            fileExplorerNodeRepository.RemoveRange(dbNodes);
        }
    }
    
    public async Task<List<DirectoryInfo>> EnsureChildrenUpdatedAsync(
        NodePath parentNodePath,
        FileSystemInfo[] children,
        FsHashBuilder hashBuilder,
        CancellationToken cancellationToken)
    {
        var root = await fileExplorerNodeRepository.GetRootChildOrThrowAsync(parentNodePath.RootPath, cancellationToken);
        var parentEntity = await fileSystemQueryService.GetFolderNodeOrDefaultAsync(parentNodePath,
            GetFolderQueryOptions.FolderOnly, cancellationToken: cancellationToken);
        
        var dbNodes = await fileSystemQueryService.GetNodesAsync(
            parentNodePath.RootPath,
            children.Select(s => rootFolder.GetNodePath(s.FullName)).ToHashSet(),
            GetFileQueryOptions.MetadataOnly, GetFolderQueryOptions.FolderOnly, cancellationToken);
        
        var directoryInfos = new List<DirectoryInfo>();
        
        foreach (var childMap in GetChildEntitiesAsync(dbNodes, children))
        {
            var addedChildren = new List<FileExplorerNodeEntity>();

            if (childMap.DbEntity is FileExplorerNodeEntity node)
            {
                node.Update(childMap.Path, childMap.FsInfo, parentEntity);
                logger.LogTrace("Updated existing child entity for {ChildPath} with ID {ChildEntityId}", childMap.Path.RelativePath, childMap.DbEntity.Id);
            }
            else 
            {
                var entity = await fileSystemInfoToEntityConverter.CreateNodeAsync(childMap.FsInfo, root, parentEntity, cancellationToken);
                childMap.DbEntity = entity;
                addedChildren.Add(entity);
                logger.LogTrace("Created new child entity for {ChildPath} with ID {ChildEntityId}", childMap.Path.RelativePath, childMap.DbEntity.Id);
            }

            hashBuilder.Add(childMap.FsInfo);
            if (childMap.FsInfo is DirectoryInfo directoryInfo)
            {
                directoryInfos.Add(directoryInfo);
            }

            await fileExplorerNodeRepository.AddRangeAsync(addedChildren, cancellationToken);
        }

        // If there are any nodes in the database that were not found in the file system, remove them.
        // Nodes are removed in GetChildEntitiesAsync, so we can just check the remaining dbNodes.
        if (dbNodes.Count > 0)
        {
            fileExplorerNodeRepository.RemoveRange(dbNodes);
        }

        return directoryInfos;
    }

    public async Task UpdateHashAsync(NodePath nodePath, string hash, CancellationToken cancellationToken)
    {
        var parentEntity = await fileSystemQueryService.GetFolderNodeOrDefaultAsync(nodePath, GetFolderQueryOptions.FolderAndChildrenWithBasicMetadata, cancellationToken: cancellationToken) ??
                           (IFileExplorerFolderEntity)await fileExplorerNodeRepository.GetRootChildOrThrowAsync(nodePath.RootPath, cancellationToken);

        parentEntity.Hash = hash;
    }

    private void NotifyChanges(List<ChildEntityMap> fsChildren, List<FileExplorerNodeEntity> removedChildren)
    {
        NotifyUpdateMediaMetadataChannel(fsChildren);
        NotifyRemoveMediaMetadataChannel(removedChildren);
    }

    private void NotifyUpdateMediaMetadataChannel(List<ChildEntityMap> fsChildren)
    {
        if (fsChildren.Count == 0)
        {
            return;
        }

        var updatedFiles = fsChildren
            .Where(w => w is { DbEntity: FileExplorerFileNodeEntity { Metadata.IsMedia: true } })
            .Select(s => s.DbEntity?.Id)
            .Where(id => id.HasValue)
            .Select(s => s!.Value)
            .ToList();

        if (updatedFiles.Count == 0)
        {
            return;
        }
        
        logger.LogDebug("Notifying update media metadata channel for {UpdatedFilesCount} files", updatedFiles.Count);
        _ = updateMediaMetadataChannel.WriteAsync(new UpdateMediaMetadataRequest(updatedFiles));
    }
    
    private void NotifyRemoveMediaMetadataChannel(List<FileExplorerNodeEntity> removedChildren)
    {
        if (removedChildren.Count == 0)
        {
            return;
        }
        
        var removedFiles = removedChildren
            .Where(w => w is FileExplorerFileNodeEntity { Metadata.IsMedia: true })
            .Select(s => new NodePath(s.RootChild.RelativePath, s.RelativePath))
            .ToList();

        if (removedFiles.Count == 0)
        {
            return;
        }
        
        logger.LogDebug("Notifying remove media metadata channel for {RemovedFilesCount} files", removedFiles.Count);
        _ = removeMediaMetadataChannel.WriteAsync(new RemoveMediaMetadataRequest(removedFiles));
    }

    private IEnumerable<ChildEntityMap> GetChildEntitiesAsync(
        HashSet<FileExplorerNodeEntity> dbChildren,
        IEnumerable<FileSystemInfo> fsChildren)
    {
        foreach (var fsChild in fsChildren)
        {
            if (dbChildren.Count == 0)
            {
                yield return new ChildEntityMap
                {
                    Path = rootFolder.GetNodePath(fsChild.FullName),
                    FsInfo = fsChild,
                    DbEntity = null,
                    IsParent = false
                };
                continue;
            }
            
            var path = rootFolder.GetNodePath(fsChild.FullName);
            var dbEntity = dbChildren.FirstOrDefault(f => f.RelativePath == path.RelativePath);
            if (dbEntity is not null)
            {
                // Remove the entity from the dbChildren set so we can identify which ones are missing later.
                dbChildren.Remove(dbEntity);
            }
            else
            {
                logger.LogWarning("File system child {ChildPath} not found in database, creating new entity", fsChild.FullName);
            }
            
            yield return new ChildEntityMap
            {
                Path = path,
                FsInfo = fsChild,
                DbEntity = dbEntity
            };
        }
    }
}