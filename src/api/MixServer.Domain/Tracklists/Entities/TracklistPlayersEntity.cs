using MixServer.Domain.Tracklists.Enums;

namespace MixServer.Domain.Tracklists.Entities;

public class TracklistPlayersEntity
{
    public required Guid Id { get; init; }
    
    public required TracklistPlayerType Type { get; set; }
    
    public required string Url { get; set; }
    
    public required TrackEntity Track { get; set; }
    public Guid TrackId { get; set; }
}