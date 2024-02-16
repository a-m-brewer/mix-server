using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.Queueing.Entities;
using MixServer.Domain.Sessions.Entities;
using MixServer.Domain.Sessions.Enums;
using MixServer.Domain.Sessions.Models;
using MixServer.Domain.Users.Entities;
using MixServer.Domain.Users.Models;

namespace MixServer.Domain.Callbacks;

public interface ICallbackService
{
    Task CurrentSessionUpdated(string userId, PlaybackSession? session);
    Task CurrentQueueUpdated(string userId, QueueSnapshot queueSnapshot);
    Task DeviceUpdated(Device device);
    Task DeviceStateUpdated(IDeviceState deviceState);
    Task FolderSorted(string userId, IFileExplorerFolder folder);
    Task DeviceDeleted(string userId, Guid deviceId);
    Task PlaybackStateUpdated(IPlaybackState playbackState, AudioPlayerStateUpdateType audioPlayerStateUpdateType);
    Task PlaybackGranted(IPlaybackState state, bool useDeviceCurrentTime);
    Task PauseRequested(Guid deviceId);
    Task UserDeleted(string userId);
    Task UserAdded(IUser user);
    Task UserUpdated(IUser user);
    Task FileExplorerNodeAdded(IFileExplorerNode node);
    Task FileExplorerNodeUpdated(IFileExplorerNode node, string oldAbsolutePath);
    Task FileExplorerNodeDeleted(IFileExplorerFolderNode parentNode, string absolutePath);
}