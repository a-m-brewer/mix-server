using MixServer.FolderIndexer.Domain.Entities;
using MixServer.Shared.Interfaces;

namespace MixServer.FolderIndexer.Converters;

public interface IFileSystemInfoConverter
    : IConverter<DirectoryInfo, DirectoryInfoEntity>,
        IUpdater<DirectoryInfoEntity, DirectoryInfo>,
        IConverter<FileInfo, FileInfoEntity>,
        IUpdater<FileInfoEntity, FileInfo>
{
    RootDirectoryInfoEntity ConvertRoot(DirectoryInfo value);
}

public class FileSystemInfoConverter : IFileSystemInfoConverter
{
    public DirectoryInfoEntity Convert(DirectoryInfo value)
    {
        return new DirectoryInfoEntity
        {
            Id = Guid.NewGuid(),
            Name = value.Name,
            AbsolutePath = value.FullName,
            Exists = value.Exists,
            CreationTimeUtc = value.CreationTimeUtc
        };
    }

    public FileInfoEntity Convert(FileInfo value)
    {
        return new FileInfoEntity
        {
            Id = Guid.NewGuid(),
            Name = value.Name,
            AbsolutePath = value.FullName,
            Exists = value.Exists,
            CreationTimeUtc = value.CreationTimeUtc,
            Extension = value.Extension
        };
    }

    public void Update(DirectoryInfoEntity value, DirectoryInfo update)
    {
        Update((FileSystemInfoEntity)value, update);
    }

    public void Update(FileInfoEntity value, FileInfo update)
    {
        Update((FileSystemInfoEntity)value, update);
        value.Extension = update.Extension;
    }
    
    public RootDirectoryInfoEntity ConvertRoot(DirectoryInfo value)
    {
        return new RootDirectoryInfoEntity
        {
            Id = Guid.NewGuid(),
            Name = value.Name,
            AbsolutePath = value.FullName,
            Exists = value.Exists,
            CreationTimeUtc = value.CreationTimeUtc
        };
    }
    
    private static void Update(FileSystemInfoEntity value, FileSystemInfo update)
    {
        value.Name = update.Name;
        value.AbsolutePath = update.FullName;
        value.Exists = update.Exists;
        value.CreationTimeUtc = update.CreationTimeUtc;
    }
}