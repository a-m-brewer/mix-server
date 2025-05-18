using MixServer.FolderIndexer.Domain.Entities;

namespace MixServer.FolderIndexer.Domain.Models;

public class RelatedDirectoryEntities<TEntity> where TEntity : FileSystemInfoEntity
{
    public required string FullName { get; init; }
    
    public required RootDirectoryInfoEntity Root { get; init; }
    
    public DirectoryInfoEntity? Parent { get; init; }
    
    public TEntity? Entity { get; init; }
}