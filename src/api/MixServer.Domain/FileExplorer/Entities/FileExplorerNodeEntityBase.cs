using System.ComponentModel.DataAnnotations.Schema;
using MixServer.Domain.FileExplorer.Enums;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.Streams.Entities;
using MixServer.Domain.Tracklists.Entities;

namespace MixServer.Domain.FileExplorer.Entities;

public interface IHasChildren
{
    List<FileExplorerNodeEntity> Children { get; set; }
}

public class FileExplorerNodeEntityBase
{
    public required Guid Id { get; init; }
    
    public required string RelativePath { get; set; }
    
    public FileExplorerEntityNodeType NodeType { get; init; }
    
    public required bool Exists { get; set; }

    public required DateTime CreationTimeUtc { get; set; }
    
    public required string Hash { get; set; } = string.Empty;
}

public class FileExplorerRootChildNodeEntity : FileExplorerNodeEntityBase, IHasChildren
{
    public List<FileExplorerNodeEntity> Children { get; set; } = [];
}

public class FileExplorerNodeEntity : FileExplorerNodeEntityBase
{
    public required FileExplorerRootChildNodeEntity RootChild { get; set; }
    
    public Guid RootChildId { get; set; }
    
    public required FileExplorerFolderNodeEntity? Parent { get; set; }
    
    public Guid? ParentId { get; set; }
    
    [NotMapped]
    public NodePath Path => new(RootChild.RelativePath, RelativePath);
}

public class FileExplorerFileNodeEntity : FileExplorerNodeEntity
{
    public Transcode? Transcode { get; set; }
    
    public TracklistEntity? Tracklist { get; set; }
    
    // TODO: Make this non-nullable after migration to file indexing is complete.
    public FileMetadataEntity? Metadata { get; set; }
    
    [NotMapped]
    public FileMetadataEntity MetadataEntity => Metadata ?? throw new InvalidOperationException("Metadata is not set.");
}

public class FileExplorerFolderNodeEntity : FileExplorerNodeEntity, IHasChildren
{
    public List<FileExplorerNodeEntity> Children { get; set; } = [];
}