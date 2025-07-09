using MixServer.Domain.FileExplorer.Entities;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Models.Metadata;
using MixServer.Domain.Queueing.Entities;
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
    Task CurrentQueueUpdated(string userId, QueueSnapshot queueSnapshot);
    Task CurrentQueueUpdated(string userId, Guid deviceId, QueueSnapshot queueSnapshot);
    Task DeviceUpdated(Device device);
    Task DeviceStateUpdated(IDeviceState deviceState);
    Task FolderSorted(string userId, IFileExplorerFolder folder);
    Task FolderRefreshed(string userId, Guid deviceId, IFileExplorerFolder folder);
    Task FolderScanStatusChanged(bool scanInProgress);
    Task DeviceDeleted(string userId, Guid deviceId);
    Task PlaybackStateUpdated(IPlaybackState playbackState, AudioPlayerStateUpdateType audioPlayerStateUpdateType);
    Task PlaybackStateUpdated(IPlaybackState state, Guid currentDeviceId, bool useDeviceCurrentTime);
    Task PlaybackGranted(IPlaybackState state, bool useDeviceCurrentTime);
    Task PauseRequested(Guid deviceId);
    Task UserDeleted(string userId);
    Task UserAdded(IUser user);
    Task UserUpdated(IUser user);
    Task FileExplorerNodeUpdated(Dictionary<string, int> expectedNodeIndexes, IFileExplorerNode node, NodePath? oldPath);
    Task FileExplorerNodeDeleted(IFileExplorerFolderNode parentNode, NodePath path);
    Task MediaInfoUpdated(IReadOnlyCollection<MediaInfo> mediaInfo);
    Task MediaInfoRemoved(IReadOnlyCollection<NodePath> removedItems);
    Task TracklistUpdated(FileExplorerFileNodeEntity file);
}