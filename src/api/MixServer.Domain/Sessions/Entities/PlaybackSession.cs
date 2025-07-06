using System.ComponentModel.DataAnnotations.Schema;
using MixServer.Domain.Extensions;
using MixServer.Domain.FileExplorer.Entities;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.Sessions.Models;

namespace MixServer.Domain.Sessions.Entities;

public interface IPlaybackSession : IPlaybackState
{
    Guid Id { get; set; }
    FileExplorerFileNodeEntity NodeEntity { get; }
    DateTime LastPlayed { get; set; }
    StreamKey StreamKey { get; set; }
    void PopulateState(IPlaybackState playingItem);
}

public class PlaybackSession : IPlaybackSession
{
    public Guid Id { get; set; }
    
    // TODO: Make this non-nullable after migration to new root, relative paths is complete.
    public FileExplorerFileNodeEntity? Node { get; set; }
    public Guid? NodeId { get; set; }

    [Obsolete("Use Node instead.")]
    public string AbsolutePath { get; set; } = string.Empty;

    public DateTime LastPlayed { get; set; }
    public TimeSpan CurrentTime { get; set; }
    
    [NotMapped]
    public bool Playing { get; set; }

    [NotMapped]
    public Guid? SessionId => Id;

    [NotMapped]
    public Guid? LastPlaybackDeviceId { get; set; }

    [NotMapped]
    public Guid? DeviceId { get; set; }

    public string UserId { get; set; } = string.Empty;

    [NotMapped]
    public StreamKey StreamKey { get; set; } = new () { Expires = 0, Key = string.Empty };
    
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

    public void PopulateState(IPlaybackState playingItem)
    {
        Playing = playingItem.Playing;
        LastPlaybackDeviceId = playingItem.LastPlaybackDeviceId;
        DeviceId = playingItem.DeviceId;
        CurrentTime = playingItem.CurrentTime;
    }
}