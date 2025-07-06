using MixServer.Domain.FileExplorer.Entities;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Services;
using MixServer.Domain.Interfaces;

namespace MixServer.Domain.FileExplorer.Converters;

public interface IFileSystemInfoToEntityConverter : IConverter
{
    FileExplorerRootChildNodeEntity CreateRootChildEntity(DirectoryInfo directoryInfo);
    Task<FileExplorerRootChildNodeEntity> CreateRootChildEntityAsync(
        DirectoryInfo directoryInfo,
        CancellationToken cancellationToken);

    Task<FileExplorerFolderNodeEntity> CreateFolderEntityAsync(
        DirectoryInfo directoryInfo,
        FileExplorerRootChildNodeEntity rootChild,
        FileExplorerFolderNodeEntity? parent,
        bool createWithHash,
        CancellationToken cancellationToken);

    Task<FileExplorerNodeEntity> CreateNodeAsync(
        FileSystemInfo child,
        FileExplorerRootChildNodeEntity root,
        FileExplorerFolderNodeEntity? parentEntity,
        CancellationToken cancellationToken);

    FileExplorerFileNodeEntity CreateFileEntity(
        FileInfo fileInfo,
        FileExplorerRootChildNodeEntity root,
        FileExplorerFolderNodeEntity? parentEntity);
}

public class FileSystemInfoToEntityConverter(
    IFileMetadataConverter fileMetadataConverter,
    IFileSystemHashService fileSystemHashService,
    IFileSystemFolderMetadataService fileSystemFolderMetadataService,
    IRootFileExplorerFolder rootFolder) : IFileSystemInfoToEntityConverter
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
        bool createWithHash,
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
            // The hash represents if the folder has been scanned for the first time or not.
            Hash = createWithHash
                ? await fileSystemHashService.ComputeFolderMd5HashAsync(directoryInfo, cancellationToken)
                : string.Empty
        };
    }

    public async Task<FileExplorerNodeEntity> CreateNodeAsync(
        FileSystemInfo child, FileExplorerRootChildNodeEntity root,
        FileExplorerFolderNodeEntity? parentEntity, CancellationToken cancellationToken)
    {
        if (child is DirectoryInfo directoryInfo) 
        {
            return await CreateFolderEntityAsync(directoryInfo, root, parentEntity, false, cancellationToken);
        }

        if (child is FileInfo fileInfo)
        {
            return CreateFileEntity(fileInfo, root, parentEntity);
        }
        
        throw new NotSupportedException($"Unsupported FileSystemInfo type: {child.GetType().Name}");
    }

    public FileExplorerFileNodeEntity CreateFileEntity(
        FileInfo fileInfo,
        FileExplorerRootChildNodeEntity root,
        FileExplorerFolderNodeEntity? parentEntity)
    {
        var nodePath = rootFolder.GetNodePath(fileInfo.FullName);

        var file = new FileExplorerFileNodeEntity
        {
            Id = Guid.NewGuid(),
            RelativePath = nodePath.RelativePath,
            Exists = fileInfo.Exists,
            CreationTimeUtc = fileInfo.CreationTimeUtc,
            RootChild = root,
            Parent = parentEntity
        };
        
        file.Metadata = fileMetadataConverter.ConvertToEntity(fileInfo, file);

        return file;
    }
}