namespace MixServer.Domain.Tracklists.Entities;

public class CueEntity
{
    public required Guid Id { get; init; }
    
    public required TimeSpan Cue { get; init; }
    
    public required TracklistEntity Tracklist { get; init; }
    public Guid TracklistId { get; set; }
    
    public List<TrackEntity> Tracks { get; set; } = [];
}