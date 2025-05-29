using MixServer.Domain.FileExplorer.Entities;

namespace MixServer.Domain.Tracklists.Entities;

public class TracklistEntity
{
    public required Guid Id { get; init; }
    
    public required List<CueEntity> Cues { get; set; } = [];
    
    public required FileExplorerFileNodeEntity Node { get; set; }
    public Guid NodeId { get; set; }
}