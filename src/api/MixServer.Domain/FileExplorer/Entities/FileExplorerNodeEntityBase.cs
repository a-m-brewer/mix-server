using System.ComponentModel.DataAnnotations.Schema;
using MixServer.Domain.FileExplorer.Enums;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.Streams.Entities;

namespace MixServer.Domain.FileExplorer.Entities;

public class FileExplorerNodeEntityBase
{
    public required Guid Id { get; init; }
    
    public required string RelativePath { get; set; }
    
    public FileExplorerEntityNodeType NodeType { get; init; }
}

public class FileExplorerRootChildNodeEntity : FileExplorerNodeEntityBase;

public class FileExplorerNodeEntity : FileExplorerNodeEntityBase
{
    public required FileExplorerRootChildNodeEntity RootChild { get; set; }
    
    public Guid RootChildId { get; set; }
    
    [NotMapped]
    public NodePath Path => new(RootChild.RelativePath, RelativePath);
}

public class FileExplorerFileNodeEntity : FileExplorerNodeEntity
{
    public Transcode? Transcode { get; set; }
}

public class FileExplorerFolderNodeEntity : FileExplorerNodeEntity;