using MixServer.Domain.FileExplorer.Entities;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Models.Metadata;
using MixServer.Domain.Queueing.Entities;
using MixServer.Domain.Queueing.Models;
using MixServer.Domain.Sessions.Entities;
using MixServer.Domain.Sessions.Enums;
using MixServer.Domain.Sessions.Models;
using MixServer.Domain.Users.Entities;
using MixServer.Domain.Users.Models;

namespace MixServer.Domain.Callbacks;

public interface ICallbackService
{
    IReadOnlyCollection<string> ConnectedUserIds { get; }
    
    Task CurrentSessionUpdated(string userId, Guid deviceId, PlaybackSession? session);
    Task DeviceUpdated(Device device);
    Task DeviceStateUpdated(IDeviceState deviceState);
    Task FolderSorted(string userId, NodePath nodePath);
    Task FolderScanned(string userId, NodePath nodePath);
    Task FolderScanStatusChanged(bool scanInProgress);
    Task DeviceDeleted(string userId, Guid deviceId);
    Task PlaybackStateUpdated(IPlaybackState playbackState, AudioPlayerStateUpdateType audioPlayerStateUpdateType);
    Task PlaybackStateUpdated(IPlaybackState state, Guid currentDeviceId, bool useDeviceCurrentTime);
    Task PlaybackGranted(IPlaybackState state, bool useDeviceCurrentTime);
    Task PauseRequested(Guid deviceId);
    Task UserDeleted(string userId);
    Task UserAdded(IUser user);
    Task UserUpdated(IUser user);
    Task MediaInfoUpdated(IReadOnlyCollection<MediaInfo> mediaInfo);
    Task MediaInfoRemoved(IReadOnlyCollection<NodePath> removedItems);
    Task TracklistUpdated(FileExplorerFileNodeEntity file);
    Task QueuePositionChanged(string userId, Guid deviceId, QueuePosition position, bool notifyCallingDevice = false);
    Task QueueFolderChanged(string userId, Guid deviceId, QueuePosition position, bool notifyCallingDevice = false);
    Task QueueItemsAdded(string userId, QueuePosition position, IEnumerable<QueueItemEntity> addedItems);
    Task QueueItemsRemoved(string userId, QueuePosition position, List<Guid> removedIds);
}