using System.Text.RegularExpressions;
using MixServer.FolderIndexer.Domain.Entities;
using MixServer.FolderIndexer.Services;
using MixServer.Shared.Interfaces;

namespace MixServer.FolderIndexer.Converters;

internal interface IFileSystemInfoConverter
    : IConverter
{
    RootDirectoryInfoEntity ConvertRoot(DirectoryInfo directoryInfo);
    void UpdateRoot(RootDirectoryInfoEntity existingRoot, DirectoryInfo directoryInfo);
    DirectoryInfoEntity ConvertChildDirectory(DirectoryInfo directoryInfo, RootDirectoryInfoEntity root, DirectoryInfoEntity? parent);

    void UpdateChildDirectory(
        DirectoryInfoEntity dir,
        DirectoryInfo directoryInfo,
        RootDirectoryInfoEntity root,
        DirectoryInfoEntity? parent);
    
    Task UpdateChildAsync(FileSystemInfoEntity fsEntity,
        FileSystemInfo fileSystemInfo,
        RootDirectoryInfoEntity root,
        DirectoryInfoEntity parent,
        CancellationToken cancellationToken);

    Task<FileSystemInfoEntity> ConvertChildAsync(FileSystemInfo fileSystemInfo, RootDirectoryInfoEntity root,
        DirectoryInfoEntity parent, CancellationToken cancellationToken);
}

internal partial class FileSystemInfoConverter(
    IFileSystemMetadataPersistenceService metadataPersistenceService) : IFileSystemInfoConverter
{
    public RootDirectoryInfoEntity ConvertRoot(DirectoryInfo directoryInfo)
    {
        return new RootDirectoryInfoEntity
        {
            Id = Guid.NewGuid(),
            Name = directoryInfo.Name,
            RelativePath = directoryInfo.FullName,
            Exists = directoryInfo.Exists,
            CreationTimeUtc = directoryInfo.CreationTimeUtc,
        };

    }

    public void UpdateRoot(RootDirectoryInfoEntity existingRoot, DirectoryInfo directoryInfo)
    {
        existingRoot.Name = directoryInfo.Name;
        existingRoot.RelativePath = directoryInfo.FullName;
        existingRoot.Exists = directoryInfo.Exists;
        existingRoot.CreationTimeUtc = directoryInfo.CreationTimeUtc;
    }

    public DirectoryInfoEntity ConvertChildDirectory(
        DirectoryInfo directoryInfo,
        RootDirectoryInfoEntity root,
        DirectoryInfoEntity? parent)
    {
        var relativePath = Path.GetRelativePath(root.RelativePath, directoryInfo.FullName);
        
        return new DirectoryInfoEntity
        {
            Id = Guid.NewGuid(),
            Name = directoryInfo.Name,
            RelativePath = relativePath,
            Exists = directoryInfo.Exists,
            CreationTimeUtc = directoryInfo.CreationTimeUtc,
            Parent = parent,
            Root = root
        };
    }

    public void UpdateChildDirectory(
        DirectoryInfoEntity dir,
        DirectoryInfo directoryInfo,
        RootDirectoryInfoEntity root,
        DirectoryInfoEntity? parent)
    {
        dir.Name = directoryInfo.Name;
        dir.RelativePath = Path.GetRelativePath(root.RelativePath, directoryInfo.FullName);
        dir.Exists = directoryInfo.Exists;
        dir.CreationTimeUtc = directoryInfo.CreationTimeUtc;
        dir.Parent = parent;
        dir.Root = root;
    }
    
    public async Task UpdateChildAsync(
        FileSystemInfoEntity fsEntity,
        FileSystemInfo fileSystemInfo,
        RootDirectoryInfoEntity root,
        DirectoryInfoEntity parent,
        CancellationToken cancellationToken)
    {
        fsEntity.Name = fileSystemInfo.Name;
        fsEntity.RelativePath = Path.GetRelativePath(root.RelativePath, fileSystemInfo.FullName);
        fsEntity.Exists = fileSystemInfo.Exists;
        fsEntity.CreationTimeUtc = fileSystemInfo.CreationTimeUtc;
        fsEntity.Parent = parent;
        fsEntity.Root = root;

        if (fileSystemInfo is FileInfo fileInfo)
        {
            await metadataPersistenceService.UpdateMetadataAsync((FileInfoEntity)fsEntity, fileInfo, cancellationToken);
        }
    }

    public async Task<FileSystemInfoEntity> ConvertChildAsync(
        FileSystemInfo fileSystemInfo,
        RootDirectoryInfoEntity root,
        DirectoryInfoEntity parent,
        CancellationToken cancellationToken)
    {
        if (fileSystemInfo is DirectoryInfo directoryInfo)
        {
            return ConvertChildDirectory(directoryInfo, root, parent);
        }
        
        if (fileSystemInfo is FileInfo fileInfo)
        {
            var file = new FileInfoEntity
            {
                Id = Guid.NewGuid(),
                Name = fileInfo.Name,
                RelativePath = Path.GetRelativePath(root.RelativePath,
                    fileInfo.FullName),
                Exists = fileInfo.Exists,
                CreationTimeUtc = fileInfo.CreationTimeUtc,
                Extension = fileInfo.Extension,
                Parent = parent,
                Root = root,
                Metadata = metadataPersistenceService.CreateMetadata(fileInfo)
            };
            file.Metadata.File = file;
            file.Metadata.FileId = file.Id;
            
            await metadataPersistenceService.AddAsync(file.Metadata, cancellationToken);
            
            return file;
        }
        
        throw new NotSupportedException($"FileSystemInfo type {fileSystemInfo.GetType()} is not supported.");
    }
}