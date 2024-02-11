using MixServer.Application.FileExplorer.Queries.GetNode;
using MixServer.Application.Queueing.Responses;
using MixServer.Application.Sessions.Dtos;
using MixServer.Application.Users.Dtos;
using MixServer.Application.Users.Responses;
using MixServer.SignalR.Events;

namespace MixServer.SignalR;

public interface ISignalRCallbackClient
{
    Task CurrentSessionUpdated(CurrentSessionUpdatedEventDto dto);
    Task CurrentQueueUpdated(QueueSnapshotDto snapshotDto);
    Task DeviceUpdated(DeviceDto dto);
    Task DeviceStateUpdated(DeviceStateDto dto);
    Task FolderSorted(FolderNodeResponse dto);
    Task DeviceDeleted(DeviceDeletedDto dto);
    Task PlaybackStateUpdated(PlaybackStateDto dto);
    Task PlaybackGranted(PlaybackGrantedDto dto);
    Task PauseRequested();
    Task UserAdded(UserDto dto);
    Task UserUpdated(UserDto convert);
    Task UserDeleted(UserDeletedDto dto);
}