using System.ComponentModel.DataAnnotations.Schema;
using MixServer.Domain.FileExplorer.Entities;

namespace MixServer.Domain.Streams.Entities;

public class Transcode
{
    public required Guid Id { get; set; }
    
    // TODO: Make this non-nullable after migration to new root, relative paths is complete.
    public FileExplorerFileNodeEntity? Node { get; set; }
    public Guid? NodeId { get; set; }
    
    [Obsolete("Use Node instead.")]
    public string AbsolutePath { get; set; } = string.Empty;
    
    // Workaround for application code to pretend Node is not nullable
    [NotMapped]
    public required FileExplorerFileNodeEntity NodeEntity
    {
        get => Node ?? throw new InvalidOperationException("Node is not set.");
        set => Node = value;
    }
    
    [NotMapped]
    public required Guid NodeIdEntity
    {
        get => NodeId ?? throw new InvalidOperationException("NodeId is not set.");
        set => NodeId = value;
    }
}