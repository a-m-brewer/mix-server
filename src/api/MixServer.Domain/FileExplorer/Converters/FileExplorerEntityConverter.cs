using MixServer.Domain.FileExplorer.Entities;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Services;
using MixServer.Domain.Interfaces;

namespace MixServer.Domain.FileExplorer.Converters;

public interface IFileExplorerEntityConverter : IConverter
{
    FileExplorerRootChildNodeEntity CreateRootChildEntity(DirectoryInfo directoryInfo);
    Task<FileExplorerRootChildNodeEntity> CreateRootChildEntityAsync(
        DirectoryInfo directoryInfo,
        CancellationToken cancellationToken);

    Task<FileExplorerFolderNodeEntity> CreateFolderEntityAsync(
        DirectoryInfo directoryInfo,
        FileExplorerRootChildNodeEntity rootChild,
        FileExplorerFolderNodeEntity? parent,
        CancellationToken cancellationToken);

    Task<FileExplorerNodeEntity> CreateNodeAsync(
        FileSystemInfo child,
        FileExplorerRootChildNodeEntity root,
        FileExplorerFolderNodeEntity? parentEntity,
        CancellationToken cancellationToken);
}

public class FileExplorerEntityConverter(
    IFileSystemHashService fileSystemHashService,
    IFileSystemFolderMetadataService fileSystemFolderMetadataService,
    IRootFileExplorerFolder rootFolder) : IFileExplorerEntityConverter
{
    public FileExplorerRootChildNodeEntity CreateRootChildEntity(DirectoryInfo directoryInfo)
    {
        return new FileExplorerRootChildNodeEntity
        {
            Id = Guid.NewGuid(),
            RelativePath = directoryInfo.FullName,
            Exists = directoryInfo.Exists,
            CreationTimeUtc = directoryInfo.CreationTimeUtc
        };
    }

    public async Task<FileExplorerRootChildNodeEntity> CreateRootChildEntityAsync(
        DirectoryInfo directoryInfo,
        CancellationToken cancellationToken)
    {
        var entity = CreateRootChildEntity(directoryInfo);
        entity.Hash = await fileSystemHashService.ComputeFolderMd5HashAsync(directoryInfo, cancellationToken);
        
        return entity;
    }

    public async Task<FileExplorerFolderNodeEntity> CreateFolderEntityAsync(
        DirectoryInfo directoryInfo,
        FileExplorerRootChildNodeEntity rootChild,
        FileExplorerFolderNodeEntity? parent,
        CancellationToken cancellationToken)
    {
        var nodePath = rootFolder.GetNodePath(directoryInfo.FullName);
        var metadata = await fileSystemFolderMetadataService.GetOrCreateAsync(nodePath, cancellationToken);

        return new FileExplorerFolderNodeEntity
        {
            Id = metadata.FolderId,
            RelativePath = nodePath.RelativePath,
            Exists = directoryInfo.Exists,
            CreationTimeUtc = directoryInfo.CreationTimeUtc,
            RootChild = rootChild,
            Parent = parent,
            Hash = await fileSystemHashService.ComputeFolderMd5HashAsync(directoryInfo, cancellationToken)
        };
    }

    public async Task<FileExplorerNodeEntity> CreateNodeAsync(
        FileSystemInfo child, FileExplorerRootChildNodeEntity root,
        FileExplorerFolderNodeEntity? parentEntity, CancellationToken cancellationToken)
    {
        if (child is DirectoryInfo directoryInfo) 
        {
            return await CreateFolderEntityAsync(directoryInfo, root, parentEntity, cancellationToken);
        }

        if (child is FileInfo fileInfo)
        {
            return await CreateFileEntityAsync(fileInfo, root, parentEntity, cancellationToken);
        }
        
        throw new NotSupportedException($"Unsupported FileSystemInfo type: {child.GetType().Name}");
    }

    private async Task<FileExplorerFileNodeEntity> CreateFileEntityAsync(
        FileInfo fileInfo,
        FileExplorerRootChildNodeEntity root,
        FileExplorerFolderNodeEntity? parentEntity,
        CancellationToken cancellationToken)
    {
        var nodePath = rootFolder.GetNodePath(fileInfo.FullName);

        return new FileExplorerFileNodeEntity
        {
            Id = Guid.NewGuid(),
            RelativePath = nodePath.RelativePath,
            Exists = fileInfo.Exists,
            CreationTimeUtc = fileInfo.CreationTimeUtc,
            RootChild = root,
            Parent = parentEntity,
            Hash = await fileSystemHashService.ComputeFileMd5HashAsync(nodePath, cancellationToken)
        };
    }
}