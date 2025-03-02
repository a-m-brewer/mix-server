using System.ComponentModel.DataAnnotations.Schema;
using MixServer.Domain.Extensions;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.Sessions.Models;

namespace MixServer.Domain.Sessions.Entities;

public interface IPlaybackSession : IPlaybackState
{
    Guid Id { get; set; }
    new string AbsolutePath { get; set; }
    DateTime LastPlayed { get; set; }
    IFileExplorerFileNode? File { get; set; }
    StreamKey StreamKey { get; set; }
    string GetParentFolderPathOrThrow();
    string? GetParentFolderPathOrDefault();
    void PopulateState(IPlaybackState playingItem);
}

public class PlaybackSession : IPlaybackSession
{
    public Guid Id { get; set; }

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
    public IFileExplorerFileNode? File { get; set; }

    [NotMapped]
    public StreamKey StreamKey { get; set; } = new () { Expires = 0, Key = string.Empty };

    public string GetParentFolderPathOrThrow()
    {
        return AbsolutePath.GetParentFolderPathOrThrow();
    }

    public string? GetParentFolderPathOrDefault()
    {
        return AbsolutePath.GetParentFolderPathOrDefault();
    }

    public void PopulateState(IPlaybackState playingItem)
    {
        Playing = playingItem.Playing;
        LastPlaybackDeviceId = playingItem.LastPlaybackDeviceId;
        DeviceId = playingItem.DeviceId;
        CurrentTime = playingItem.CurrentTime;
    }
}