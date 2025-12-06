using MixServer.Application.Devices.Responses;
using MixServer.Application.FileExplorer.Dtos;
using MixServer.Application.FileExplorer.Queries.GetNode;
using MixServer.Application.Queueing.Responses;
using MixServer.Application.Sessions.Dtos;
using MixServer.Application.Users.Dtos;
using MixServer.SignalR.Events;

namespace MixServer.SignalR;

public interface ISignalRCallbackClient
{
    Task CurrentSessionUpdated(CurrentSessionUpdatedEventDto dto);
    Task DeviceUpdated(DeviceDto dto);
    Task DeviceStateUpdated(DeviceStateDto dto);
    Task FolderScanned(ResetFolderDto dto);
    Task FolderSorted(ResetFolderDto dto);
    Task FolderScanStatusChanged(FolderScanStatusDto dto);
    Task DeviceDeleted(DeviceDeletedDto dto);
    Task PlaybackStateUpdated(PlaybackStateDto dto);
    Task PlaybackGranted(PlaybackGrantedDto dto);
    Task PauseRequested();
    Task UserAdded(UserDto dto);
    Task UserUpdated(UserDto dto);
    Task UserDeleted(UserDeletedDto dto);
    Task MediaInfoUpdated(MediaInfoUpdatedDto dto);
    Task MediaInfoRemoved(MediaInfoRemovedDto dto);
    Task TracklistUpdated(TracklistUpdatedDto dto);
    Task QueuePositionChanged(QueuePositionDto dto);
    Task QueueFolderChanged(QueuePositionDto dto);
    Task QueueItemsAdded(QueueItemsAddedDto dto);
    Task QueueItemsRemoved(QueueItemsRemovedDto dto);
}