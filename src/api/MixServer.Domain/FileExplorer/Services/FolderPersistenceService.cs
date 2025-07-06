using Microsoft.Extensions.Logging;
using MixServer.Domain.Extensions;
using MixServer.Domain.FileExplorer.Converters;
using MixServer.Domain.FileExplorer.Entities;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Repositories;
using MixServer.Domain.FileExplorer.Repositories.DbQueryOptions;
using MixServer.Domain.Persistence;

namespace MixServer.Domain.FileExplorer.Services;

public interface IFolderPersistenceService
{
    Task<IFileExplorerFolderEntity> GetOrAddFolderAsync(NodePath nodePath, CancellationToken cancellationToken = default);
    Task<FileExplorerFileNodeEntity> GetOrAddFileAsync(NodePath nodePath, CancellationToken cancellationToken = default);
    Task<List<FileExplorerFileNodeEntity>>  GetOrAddFileRangeAsync(IReadOnlyList<NodePath> nodePaths, CancellationToken cancellationToken = default);
    Task<IFileExplorerFolderEntity> AddOrUpdateFolderAsync(NodePath directoryPath,
        DirectoryInfo directory,
        List<FileSystemInfo> children,
        CancellationToken cancellationToken = default);
}

public class FolderPersistenceService(
    IFileSystemInfoToEntityConverter fileSystemInfoToEntityConverter,
    IFileExplorerNodeRepository fileExplorerNodeRepository,
    IFileSystemHashService fileSystemHashService,
    IFileSystemQueryService fileSystemQueryService,
    ILogger<FolderPersistenceService> logger,
    IRemoveMediaMetadataChannel removeMediaMetadataChannel,
    IRootFileExplorerFolder rootFolder,
    IUpdateMediaMetadataChannel updateMediaMetadataChannel,
    IUnitOfWork unitOfWork) : IFolderPersistenceService
{
    private class ChildEntityMap
    {
        public required NodePath Path { get; init; } 
        public required FileSystemInfo FsInfo { get; init; }
        public required FileExplorerNodeEntity? DbEntity { get; set; }
    }

    public async Task<IFileExplorerFolderEntity> GetOrAddFolderAsync(NodePath nodePath, CancellationToken cancellationToken = default)
    {
        var existingFolder = await fileSystemQueryService.GetRootChildOrFolderNodeOrDefaultAsync(nodePath, GetFolderQueryOptions.Full, cancellationToken);

        if (existingFolder is not null && !string.IsNullOrWhiteSpace(existingFolder.Hash))
        {
            return existingFolder;
        }
        
        var directory = new DirectoryInfo(nodePath.AbsolutePath);
        
        var children = new List<FileSystemInfo>();
        try
        {
            children = directory.MsEnumerateFileSystemInfos().ToList();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to enumerate children for directory {DirectoryPath}", directory.FullName);
        }
        
        return await AddOrUpdateFolderAsync(nodePath, directory, children, cancellationToken);
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

    public async Task<IFileExplorerFolderEntity> AddOrUpdateFolderAsync(NodePath directoryPath, DirectoryInfo directory, List<FileSystemInfo> children,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Received persist folder request for {Directory} with {ChildrenCount} children",
            directory.FullName, children.Count);
        
        var root = await fileExplorerNodeRepository.GetRootChildOrThrowAsync(directoryPath.RootPath, cancellationToken);
        var dirHash = await fileSystemHashService.ComputeFolderMd5HashAsync(directory, cancellationToken);
        
        var parentEntity = await EnsureParentUpdatedAsync(
            directoryPath,
            directory,
            dirHash,
            root,
            cancellationToken);
        
        var fsChildren = await EnsureChildrenUpdatedAsync(
            children,
            root,
            parentEntity,
            cancellationToken);

        var removedChildren = RemoveChildrenMissingInFileSystem(parentEntity is null ? root.Children : parentEntity.Children, fsChildren);

        unitOfWork.OnSaved(() => NotifyChanges(fsChildren, removedChildren));

        logger.LogInformation("Persisted folder {Directory} with {ChildrenCount} children, removed {RemovedChildrenCount} children",
            directory.FullName, fsChildren.Count, removedChildren.Count);

        IFileExplorerFolderEntity parentEntityOrRoot = parentEntity is null ? root : parentEntity;
        
        return parentEntityOrRoot;
    }
    
    private async Task<FileExplorerFolderNodeEntity?> EnsureParentUpdatedAsync(
        NodePath directoryPath,
        DirectoryInfo directory,
        string directoryHash,
        FileExplorerRootChildNodeEntity root,
        CancellationToken cancellationToken)
    {
        if (directoryPath.IsRootChild)
        {
            root.Update(directoryPath, directory, directoryHash);
            logger.LogTrace("Directory path {DirectoryPath} is root child, updated root entity", directoryPath.AbsolutePath);
            return null;
        }
        
        var grandParentPath = directoryPath.Parent;
        var grandParentEntity = grandParentPath.IsRootChild
            ? null
            : await fileSystemQueryService.GetFolderNodeOrDefaultAsync(grandParentPath, GetFolderQueryOptions.FolderOnly, cancellationToken);
            
        var parentEntity = await fileSystemQueryService.GetFolderNodeOrDefaultAsync(directoryPath, GetFolderQueryOptions.FolderAndChildrenWithBasicMetadata, cancellationToken: cancellationToken);
        if (parentEntity is null)
        {
            parentEntity = await fileSystemInfoToEntityConverter.CreateFolderEntityAsync(
                directory,
                root,
                grandParentEntity,
                true,
                cancellationToken);
            await fileExplorerNodeRepository.AddAsync(parentEntity, cancellationToken);
            logger.LogTrace("Directory path {DirectoryPath} is not in database, created new folder entity", directoryPath.AbsolutePath);
        }
        else
        {
            parentEntity.Update(directoryPath, directory, grandParentEntity);
            parentEntity.Hash = directoryHash; // Only update the hash if it's the directory being processed and not a child.
            logger.LogTrace("Directory path {DirectoryPath} is in database, updated existing folder entity", directoryPath.AbsolutePath);
        }

        logger.LogDebug("Ensured parent entity exists and is up to date for {DirectoryPath} with ID {ParentEntityId}", directoryPath.AbsolutePath, parentEntity.Id);
        return parentEntity;
    }

    private async Task<List<ChildEntityMap>> EnsureChildrenUpdatedAsync(
        List<FileSystemInfo> children,
        FileExplorerRootChildNodeEntity root,
        FileExplorerFolderNodeEntity? parentEntity,
        CancellationToken cancellationToken)
    {
        var fsChildren = await GetChildEntitiesAsync(root.RelativePath, children, cancellationToken);

        var addedChildren = new List<Task<FileExplorerNodeEntity>>();
        
        foreach (var fsChild in fsChildren)
        {
            if (fsChild.DbEntity is null)
            {
                async Task<FileExplorerNodeEntity> CreateFunc()
                {
                    fsChild.DbEntity = await fileSystemInfoToEntityConverter.CreateNodeAsync(fsChild.FsInfo, root, parentEntity, cancellationToken);

                    logger.LogDebug("Created new child entity for {ChildPath} with ID {ChildEntityId}", fsChild.Path.RelativePath, fsChild.DbEntity.Id);

                    return fsChild.DbEntity;
                }

                addedChildren.Add(CreateFunc());
            }
            else 
            {
                fsChild.DbEntity.Update(fsChild.Path, fsChild.FsInfo, parentEntity);
                logger.LogDebug("Updated existing child entity for {ChildPath} with ID {ChildEntityId}", fsChild.Path.RelativePath, fsChild.DbEntity.Id);
            }
        }

        var addedChildrenResults = await Task.WhenAll(addedChildren);
        await fileExplorerNodeRepository.AddRangeAsync(addedChildrenResults, cancellationToken);
        logger.LogDebug("Ensured {ChildrenCount} children entities are up to date for {ParentPath}",
            fsChildren.Count, parentEntity?.RelativePath ?? root.RelativePath);
        return fsChildren;
    }
    
    private List<FileExplorerNodeEntity> RemoveChildrenMissingInFileSystem(
        List<FileExplorerNodeEntity> dbChildren,
        List<ChildEntityMap> fsChildren)
    {
        var fsChildPaths = fsChildren.Select(s => s.Path.RelativePath)
            .ToHashSet();
        var removedChildren = dbChildren
            .Where(db => fsChildPaths.All(fsRelativePath => fsRelativePath != db.RelativePath))
            .ToList();
        fileExplorerNodeRepository.RemoveRange(removedChildren);

        logger.LogDebug("Removed {RemovedChildrenCount} children that are missing in file system", removedChildren.Count);
        return removedChildren;
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

    private async Task<List<ChildEntityMap>> GetChildEntitiesAsync(
        string rootPath,
        List<FileSystemInfo> children,
        CancellationToken cancellationToken)
    {
        var fsChildPaths = children
            .Select(s => new { NodePath = rootFolder.GetNodePath(s.FullName), Info = s })
            .ToList();

        var childEntities = await fileSystemQueryService
            .GetNodesAsync(
                rootPath,
                fsChildPaths.Select(s => s.NodePath).ToList(), new GetFolderQueryOptions(), GetFileQueryOptions.MetadataOnly, cancellationToken);

        return (from fs in fsChildPaths
            join dbChildEntity in childEntities on fs.NodePath.RelativePath equals dbChildEntity.RelativePath into dbEntityGroup
            from dbChildEntity in dbEntityGroup.DefaultIfEmpty()
            select new ChildEntityMap
            {
                Path = fs.NodePath,
                FsInfo = fs.Info,
                DbEntity = dbChildEntity
            }).ToList();
    }
}