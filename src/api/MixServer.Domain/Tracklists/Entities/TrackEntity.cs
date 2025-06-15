namespace MixServer.Domain.Tracklists.Entities;

public class TrackEntity
{
    public required Guid Id { get; init; }
    
    public required string Name { get; init; }
    
    public required string Artist { get; init; }
    
    public required CueEntity Cue { get; init; }
    public Guid CueId { get; set; }
    
    public List<TracklistPlayersEntity> Players { get; set; } = [];
}