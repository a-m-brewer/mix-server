using MixServer.FolderIndexer.Domain.Entities;

namespace MixServer.FolderIndexer.Domain.Models;

public class RelatedDirectoryEntities
{
    public required string FullName { get; init; }
    
    public required RootDirectoryInfoEntity Root { get; init; }
    
    public DirectoryInfoEntity? Parent { get; init; }
    
    public DirectoryInfoEntity? Directory { get; init; }
}