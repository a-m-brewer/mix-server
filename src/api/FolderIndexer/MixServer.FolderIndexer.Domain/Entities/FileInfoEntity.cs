using MixServer.FolderIndexer.Domain.Exceptions;
using MixServer.FolderIndexer.Interface.Models;

namespace MixServer.FolderIndexer.Domain.Entities;

public class FileInfoEntity : FileSystemInfoEntity, IFileInfo
{
    public required string Extension { get; set; } = string.Empty;

    public required FileMetadataEntity Metadata { get; set; }
    
    public IDirectoryInfo ParentDirectory => Parent ?? throw new FolderIndexerEntityNotFoundException(nameof(Parent), "Parent directory not found");
    public IRootDirectoryInfo RootDirectory => Root ?? throw new FolderIndexerEntityNotFoundException(nameof(Root), "Root directory not found");
}