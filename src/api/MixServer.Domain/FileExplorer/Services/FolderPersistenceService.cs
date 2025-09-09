using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using MixServer.Domain.Extensions;
using MixServer.Domain.FileExplorer.Converters;
using MixServer.Domain.FileExplorer.Entities;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Repositories;
using MixServer.Domain.FileExplorer.Repositories.DbQueryOptions;

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
    Task<IFileExplorerFolderEntity> GetOrAddFolderAsync(NodePath nodePath,
        Page page,
        IFolderSort sort,
        CancellationToken cancellationToken = default);

    Task<FileExplorerFileNodeEntity> GetOrAddFileAsync(NodePath nodePath, CancellationToken cancellationToken = default);

    Task<List<FileExplorerFileNodeEntity>>  GetOrAddFileRangeAsync(IReadOnlyList<NodePath> nodePaths, CancellationToken cancellationToken = default);

    Task<FileExplorerFolderNodeEntity?> EnsureParentUpdatedAsync(
        NodePath directoryPath,
        DirectoryInfo directory,
        CancellationToken cancellationToken);

    Task<List<FileSystemInfo>> EnsureChildrenUpdatedAsync(
        NodePath parentNodePath,
        DirectoryInfo parentDirectory,
        Page page,
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
    IUpdateMediaMetadataChannel updateMediaMetadataChannel) : IFolderPersistenceService
{

    public async Task<IFileExplorerFolderEntity> GetOrAddFolderAsync(NodePath nodePath,
        Page page,
        IFolderSort sort,
        CancellationToken cancellationToken = default)
    {
        var existingFolder = await fileSystemQueryService.GetRootChildOrFolderNodeOrDefaultAsync(nodePath, GetFolderQueryOptions.Full(page, sort), cancellationToken);

        if (existingFolder is not null && !string.IsNullOrWhiteSpace(existingFolder.Hash))
        {
            return existingFolder;
        }
        
        var directory = new DirectoryInfo(nodePath.AbsolutePath);
        
        var root = await fileExplorerNodeRepository.GetRootChildOrThrowAsync(nodePath.RootPath, cancellationToken);
        
        var parentEntity = await EnsureParentUpdatedAsync(
            nodePath,
            directory,
            root,
            cancellationToken);
        
        await EnsureChildrenUpdatedAsync(
            nodePath.Parent,
            directory,
            page,
            sort,
            null,
            root,
            new Result<FileExplorerFolderNodeEntity>(true, parentEntity),
            cancellationToken);
        
        IFileExplorerFolderEntity parentEntityOrRoot = parentEntity is null ? root : parentEntity;

        return parentEntityOrRoot;
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

    public async Task<List<FileSystemInfo>> EnsureChildrenUpdatedAsync(
        NodePath parentNodePath,
        DirectoryInfo parentDirectory,
        Page page,
        FsHashBuilder hashBuilder,
        CancellationToken cancellationToken)
    {
        var root = await fileExplorerNodeRepository.GetRootChildOrThrowAsync(parentNodePath.RootPath, cancellationToken);
        var parentEntity = await fileSystemQueryService.GetFolderNodeOrDefaultAsync(parentNodePath, GetFolderQueryOptions.FolderOnly, cancellationToken: cancellationToken);

        return await EnsureChildrenUpdatedAsync(parentNodePath,
            parentDirectory,
            page,
            null,
            hashBuilder,
            root,
            new Result<FileExplorerFolderNodeEntity>(true, parentEntity),
            cancellationToken);
    }
    
    private async Task<List<FileSystemInfo>> EnsureChildrenUpdatedAsync(
        NodePath parentNodePath,
        DirectoryInfo parentDirectory,
        Page page,
        IFolderSort? sort = null,
        FsHashBuilder? hashBuilder = null,
        FileExplorerRootChildNodeEntity? rootEntity = null,
        Result<FileExplorerFolderNodeEntity>? parentResult = null,
        CancellationToken cancellationToken = default)
    {
        var children = parentDirectory.MsEnumerateFileSystem<FileSystemInfo>(page, sort)
            .ToList();
        
        var root = rootEntity ?? await fileExplorerNodeRepository.GetRootChildOrThrowAsync(parentNodePath.RootPath, cancellationToken);;
        var parentEntity = parentResult is { Success: true }
            ? parentResult.Value
            : await fileSystemQueryService.GetFolderNodeOrDefaultAsync(parentNodePath, GetFolderQueryOptions.FolderOnly, cancellationToken: cancellationToken);
        
        var dbNodes = await fileSystemQueryService.GetNodesAsync(
            parentNodePath.RootPath,
            children.Select(s => rootFolder.GetNodePath(s.FullName)).ToHashSet(),
            GetFileQueryOptions.MetadataOnly, GetFolderQueryOptions.FolderOnly, cancellationToken);
        
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

            hashBuilder?.Add(childMap.FsInfo);

            await fileExplorerNodeRepository.AddRangeAsync(addedChildren, cancellationToken);
        }

        // If there are any nodes in the database that were not found in the file system, remove them.
        // Nodes are removed in GetChildEntitiesAsync, so we can just check the remaining dbNodes.
        if (dbNodes.Count > 0)
        {
            fileExplorerNodeRepository.RemoveRange(dbNodes);
        }

        return children;
    }

    public async Task UpdateHashAsync(NodePath nodePath, string hash, CancellationToken cancellationToken)
    {
        var parentEntity = await fileSystemQueryService.GetFolderNodeOrDefaultAsync(nodePath, GetFolderQueryOptions.FolderOnly, cancellationToken: cancellationToken) ??
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

    private record Result<T>(bool Success, T? Value);
}