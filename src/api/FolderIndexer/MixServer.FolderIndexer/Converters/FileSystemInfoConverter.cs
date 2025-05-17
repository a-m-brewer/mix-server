using MixServer.FolderIndexer.Domain.Entities;
using MixServer.Shared.Interfaces;

namespace MixServer.FolderIndexer.Converters;

public interface IFileSystemInfoConverter
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
    
    void UpdateChild(
        FileSystemInfoEntity fsEntity, 
        FileSystemInfo fileSystemInfo,
        RootDirectoryInfoEntity root,
        DirectoryInfoEntity parent);

    FileSystemInfoEntity ConvertChild(FileSystemInfo fileSystemInfo, RootDirectoryInfoEntity root, DirectoryInfoEntity parent);
}

public class FileSystemInfoConverter : IFileSystemInfoConverter
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
    
    public void UpdateChild(
        FileSystemInfoEntity fsEntity,
        FileSystemInfo fileSystemInfo,
        RootDirectoryInfoEntity root,
        DirectoryInfoEntity parent)
    {
        fsEntity.Name = fileSystemInfo.Name;
        fsEntity.RelativePath = Path.GetRelativePath(root.RelativePath, fileSystemInfo.FullName);
        fsEntity.Exists = fileSystemInfo.Exists;
        fsEntity.CreationTimeUtc = fileSystemInfo.CreationTimeUtc;
        fsEntity.Parent = parent;
        fsEntity.Root = root;

        if (fileSystemInfo is FileInfo fileInfo)
        {
            ((FileInfoEntity)fsEntity).Extension = fileInfo.Extension;
        }
    }

    public FileSystemInfoEntity ConvertChild(
        FileSystemInfo fileSystemInfo,
        RootDirectoryInfoEntity root,
        DirectoryInfoEntity parent)
    {
        if (fileSystemInfo is DirectoryInfo directoryInfo)
        {
            return ConvertChildDirectory(directoryInfo, root, parent);
        }
        
        if (fileSystemInfo is FileInfo fileInfo)
        {
            return new FileInfoEntity
            {
                Id = Guid.NewGuid(),
                Name = fileInfo.Name,
                RelativePath = Path.GetRelativePath(root.RelativePath, fileInfo.FullName),
                Exists = fileInfo.Exists,
                CreationTimeUtc = fileInfo.CreationTimeUtc,
                Extension = fileInfo.Extension,
                Parent = parent,
                Root = root
            };
        }
        
        throw new NotSupportedException($"FileSystemInfo type {fileSystemInfo.GetType()} is not supported.");
    }
}