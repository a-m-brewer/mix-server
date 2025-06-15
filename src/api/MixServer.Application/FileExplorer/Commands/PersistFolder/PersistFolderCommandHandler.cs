using Microsoft.Extensions.Logging;
using MixServer.Domain.FileExplorer.Converters;
using MixServer.Domain.FileExplorer.Entities;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Repositories;
using MixServer.Domain.FileExplorer.Services;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Persistence;
using MixServer.Domain.Sessions.Repositories;

namespace MixServer.Application.FileExplorer.Commands.PersistFolder;

public class PersistFolderCommandHandler(
    IFileExplorerEntityConverter fileExplorerEntityConverter,
    IFolderExplorerNodeEntityRepository folderExplorerNodeEntityRepository,
    IFileSystemHashService fileSystemHashService,
    IFileSystemQueryService fileSystemQueryService,
    ILogger<PersistFolderCommandHandler> logger,
    IRemoveMediaMetadataChannel removeMediaMetadataChannel,
    IRootFileExplorerFolder rootFolder,
    IUpdateMediaMetadataChannel updateMediaMetadataChannel,
    IUnitOfWork unitOfWork) : ICommandHandler<PersistFolderCommand>
{
    private class ChildEntityMap
    {
        public required NodePath Path { get; init; } 
        public required FileSystemInfo FsInfo { get; init; }
        public required FileExplorerNodeEntity? DbEntity { get; set; }
    }
    
    public async Task HandleAsync(PersistFolderCommand request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Received persist folder request for {Directory} with {ChildrenCount} children",
            request.Directory.FullName, request.Children.Count);
        
        var root = await folderExplorerNodeEntityRepository.GetRootChildOrThrowAsync(request.DirectoryPath.RootPath, cancellationToken);
        var dirHash = await fileSystemHashService.ComputeFolderMd5HashAsync(request.Directory, cancellationToken);
        
        var parentEntity = await EnsureParentUpdatedAsync(
            request.DirectoryPath,
            request.Directory,
            dirHash,
            root,
            cancellationToken);
        
        var fsChildren = await EnsureChildrenUpdatedAsync(
            request.Children,
            root,
            parentEntity,
            cancellationToken);

        var removedChildren = RemoveChildrenMissingInFileSystem(parentEntity is null ? root.Children : parentEntity.Children, fsChildren);

        NotifyChangesOnSaved(fsChildren, removedChildren);
        
        await unitOfWork.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Persisted folder {Directory} with {ChildrenCount} children, removed {RemovedChildrenCount} children",
            request.Directory.FullName, fsChildren.Count, removedChildren.Count);
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
            : await fileSystemQueryService.GetFolderNodeOrDefaultAsync(grandParentPath, false, cancellationToken);
            
        var parentEntity = await fileSystemQueryService.GetFolderNodeOrDefaultAsync(directoryPath, cancellationToken: cancellationToken);
        if (parentEntity is null)
        {
            parentEntity = await fileExplorerEntityConverter.CreateFolderEntityAsync(
                directory,
                root,
                grandParentEntity,
                cancellationToken);
            await folderExplorerNodeEntityRepository.AddAsync(parentEntity, cancellationToken);
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
                    fsChild.DbEntity = await fileExplorerEntityConverter.CreateNodeAsync(fsChild.FsInfo, root, parentEntity, cancellationToken);

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
        await folderExplorerNodeEntityRepository.AddRangeAsync(addedChildrenResults, cancellationToken);
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
        folderExplorerNodeEntityRepository.RemoveRange(removedChildren);

        logger.LogDebug("Removed {RemovedChildrenCount} children that are missing in file system", removedChildren.Count);
        return removedChildren;
    }
    
    private void NotifyChangesOnSaved(List<ChildEntityMap> fsChildren, List<FileExplorerNodeEntity> removedChildren)
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
                fsChildPaths.Select(s => s.NodePath).ToList(),
                cancellationToken);

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