using Microsoft.AspNetCore.SignalR;
using MixServer.Application.Devices.Responses;
using MixServer.Application.FileExplorer.Converters;
using MixServer.Application.FileExplorer.Dtos;
using MixServer.Application.FileExplorer.Queries.GetNode;
using MixServer.Application.Queueing.Converters;
using MixServer.Application.Sessions.Converters;
using MixServer.Application.Sessions.Responses;
using MixServer.Application.Users.Dtos;
using MixServer.Domain.Callbacks;
using MixServer.Domain.FileExplorer.Entities;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Models.Metadata;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Queueing.Entities;
using MixServer.Domain.Queueing.Models;
using MixServer.Domain.Sessions.Entities;
using MixServer.Domain.Sessions.Enums;
using MixServer.Domain.Sessions.Models;
using MixServer.Domain.Tracklists.Converters;
using MixServer.Domain.Users.Entities;
using MixServer.Domain.Users.Enums;
using MixServer.Domain.Users.Models;
using MixServer.SignalR.Events;

namespace MixServer.SignalR;

public class SignalRCallbackService(
    IConverter<IDevice, DeviceDto> deviceDtoConverter,
    IConverter<IDeviceState, DeviceStateDto> deviceStateConverter,
    IFileExplorerResponseConverter fileExplorerResponseConverter,
    IMediaInfoDtoConverter mediaInfoDtoConverter,
    INodePathDtoConverter nodePathDtoConverter,
    IConverter<IPlaybackSession, bool, PlaybackSessionDto> playbackSessionConverter,
    IPlaybackStateConverter playbackStateConverter,
    ITracklistDtoConverter tracklistDtoConverter,
    IHubContext<SignalRCallbackHub, ISignalRCallbackClient> context,
    IConverter<IUser, UserDto> userDtoConverter,
    ISignalRUserManager userManager,
    IQueueDtoConverter queueDtoConverter)
    : ICallbackService
{
    public IReadOnlyCollection<string> ConnectedUserIds => userManager.ConnectedUsers.Select(user => user.SignalRUserId.Id).ToList();

    public async Task CurrentSessionUpdated(string userId, Guid deviceId, PlaybackSession? session)
    {
        var currentSessionDto = session == null
            ? null
            : playbackSessionConverter.Convert(session, true);
        
        await context.Clients
            .Clients(GetDeviceConnectionsExcept(userId, deviceId))
            .CurrentSessionUpdated(new CurrentSessionUpdatedEventDto
            {
                CurrentPlaybackSession = currentSessionDto
            });
    }

    public async Task DeviceUpdated(Device device)
    {
        if (string.IsNullOrWhiteSpace(device.LastInteractedWith))
        {
            return;
        }
        
        var clients = userManager.GetConnectionsInGroups(new SignalRGroup(device.LastInteractedWith));

        var dto = deviceDtoConverter.Convert(device);
        
        await context.Clients
            .Clients(clients)
            .DeviceUpdated(dto);
    }

    public async Task DeviceStateUpdated(IDeviceState deviceState)
    {
        if (string.IsNullOrWhiteSpace(deviceState.LastInteractedWith))
        {
            return;
        }
        
        var clients = userManager.GetConnectionsInGroups(new SignalRGroup(deviceState.LastInteractedWith));

        var dto = deviceStateConverter.Convert(deviceState);

        await context.Clients
            .Clients(clients)
            .DeviceStateUpdated(dto);
    }

    public async Task FolderSorted(string userId, IFileExplorerFolderPage folder)
    {
        var clients = userManager.GetConnectionsInGroups(new SignalRGroup(userId));

        var dto = fileExplorerResponseConverter.Convert(folder);

        await context.Clients
            .Clients(clients)
            .FolderSorted(dto);
    }

    public async Task FolderRefreshed(string userId, Guid deviceId, IFileExplorerFolderPage folder)
    {
        // Current Device is notified via RefreshFolderCommandHandler
        var (_, otherDevicesConnections) = GetDevicesConnectionWithOtherDevices(userId, deviceId);

        var dto = fileExplorerResponseConverter.Convert(folder);

        await context.Clients
            .Clients(otherDevicesConnections)
            .FolderRefreshed(dto);
    }

    public Task FolderScanStatusChanged(bool scanInProgress)
    {
        return context.Clients
            .All
            .FolderScanStatusChanged(new FolderScanStatusDto
            {
                ScanInProgress = scanInProgress
            });
    }

    public async Task DeviceDeleted(string userId, Guid deviceId)
    {
        var clients = userManager.GetConnectionsInGroups(new SignalRGroup(userId));

        await context.Clients
            .Clients(clients)
            .DeviceDeleted(new DeviceDeletedDto
            {
                DeviceId = deviceId
            });
    }

    public async Task PlaybackStateUpdated(IPlaybackState playbackState, AudioPlayerStateUpdateType type)
    {
        var (deviceConnections, otherDevicesConnections) =
            GetDevicesConnectionWithOtherDevices(playbackState.UserId, playbackState.DeviceId);

        var dto = playbackStateConverter.Convert(playbackState, type);

        if (type is AudioPlayerStateUpdateType.Seek)
        {
            await context.Clients
                .Clients(deviceConnections)
                .PlaybackStateUpdated(dto);
        }
        
        await context.Clients
            .Clients(otherDevicesConnections)
            .PlaybackStateUpdated(dto);
    }

    public async Task PlaybackStateUpdated(IPlaybackState state, Guid currentDeviceId, bool useDeviceCurrentTime)
    {
        await context.Clients
            .Clients(GetDeviceConnectionsExcept(state.UserId, state.DeviceId, currentDeviceId))
            .PlaybackStateUpdated(playbackStateConverter.Convert(state, useDeviceCurrentTime));
    }

    public async Task PlaybackGranted(IPlaybackState state, bool useDeviceCurrentTime)
    {
        var deviceConnections = GetDeviceConnections(state.DeviceId);
        
        var dto = playbackStateConverter.Convert(state, useDeviceCurrentTime);
        
        await context.Clients
            .Clients(deviceConnections)
            .PlaybackGranted(dto);
    }

    public async Task PauseRequested(Guid deviceId)
    {
        var deviceConnections = userManager.GetConnectionsInGroups(new SignalRGroup(deviceId.ToString()));

        await context.Clients
            .Clients(deviceConnections)
            .PauseRequested();
    }

    public async Task UserAdded(IUser user)
    {
        var connections = userManager.GetConnectionsInGroups(
            new SignalRGroup(Role.Administrator.ToString()),
            new SignalRGroup(Role.Owner.ToString()));
        
        await context.Clients
            .Clients(connections)
            .UserAdded(userDtoConverter.Convert(user));
    }

    public Task UserUpdated(IUser user)
    {
        var connections = userManager.GetConnectionsInGroups(
            new SignalRGroup(Role.Administrator.ToString()),
            new SignalRGroup(Role.Owner.ToString()));
        
        return context.Clients
            .Clients(connections)
            .UserUpdated(userDtoConverter.Convert(user));
    }

    public Task MediaInfoUpdated(IReadOnlyCollection<MediaInfo> mediaInfo)
    {
        var eventDto = new MediaInfoUpdatedDto
        {
            MediaInfo = mediaInfo.Select(mediaInfoDtoConverter.Convert).ToList()
        };
        
        return context.Clients.All.MediaInfoUpdated(eventDto);
    }

    public Task MediaInfoRemoved(IReadOnlyCollection<NodePath> removedItems)
    {
        var eventDto = new MediaInfoRemovedDto
        {
            NodePaths = removedItems.Select(nodePathDtoConverter.ConvertToResponse).ToList()
        };
        
        return context.Clients.All.MediaInfoRemoved(eventDto);
    }

    public async Task TracklistUpdated(FileExplorerFileNodeEntity file)
    {
        var tracklistDto = tracklistDtoConverter.Convert(file.Tracklist);
        var dto = new TracklistUpdatedDto
        {
            Path = nodePathDtoConverter.ConvertToResponse(file.Path),
            Tracklist = tracklistDto
        };
        
        await context.Clients
            .All
            .TracklistUpdated(dto);
    }

    public Task QueuePositionChanged(string userId, Guid deviceId, QueuePosition position, bool notifyCallingDevice = false)
    {
        var (deviceConnections, otherDevicesConnections) =
            GetDevicesConnectionWithOtherDevices(userId, deviceId);

        var dto = queueDtoConverter.Convert(position);
        
        var toNotify = notifyCallingDevice
            ? deviceConnections.Concat(otherDevicesConnections).ToList()
            : otherDevicesConnections;
        
        return context.Clients
            .Clients(toNotify)
            .QueuePositionChanged(dto);
    }

    public Task QueueFolderChanged(string userId, Guid deviceId, QueuePosition position, bool notifyCallingDevice = false)
    {
        var (deviceConnections, otherDevicesConnections) =
            GetDevicesConnectionWithOtherDevices(userId, deviceId);

        var dto = queueDtoConverter.Convert(position);
        
        var toNotify = notifyCallingDevice
            ? deviceConnections.Concat(otherDevicesConnections).ToList()
            : otherDevicesConnections;
        
        return context.Clients
            .Clients(toNotify)
            .QueueFolderChanged(dto);
    }

    public Task QueueItemsAdded(string userId, QueuePosition position, IEnumerable<QueueItemEntity> addedItems)
    {
        return context.Clients
            .Clients(userManager.GetConnectionsInGroups(new SignalRGroup(userId)))
            .QueueItemsAdded(queueDtoConverter.Convert(addedItems, position));
    }

    public Task QueueItemsRemoved(string userId, QueuePosition position, List<Guid> removedIds)
    {
        return context.Clients
            .Clients(userManager.GetConnectionsInGroups(new SignalRGroup(userId)))
            .QueueItemsRemoved(queueDtoConverter.Convert(removedIds, position));
    }

    public async Task UserDeleted(string userId)
    {
        var connections = userManager.GetConnectionsInGroups(
            new SignalRGroup(Role.Administrator.ToString()),
            new SignalRGroup(Role.Owner.ToString()));
        
        await context.Clients
            .Clients(connections)
            .UserDeleted(new UserDeletedDto { UserId = userId });
    }
    
    private 
        (IReadOnlyList<SignalRConnectionId> CurrentDeviceConnections, IReadOnlyList<SignalRConnectionId> OtherDevicesConnections)
        GetDevicesConnectionWithOtherDevices(string userId, Guid? deviceId)
    {
        var deviceConnections = GetDeviceConnections(deviceId);
        var otherDevices = GetDeviceConnectionsExcept(userId, deviceId);

        return (deviceConnections, otherDevices);
    }

    private IReadOnlyList<SignalRConnectionId> GetDeviceConnectionsExcept(string userId, params Guid?[] deviceIds)
    {
        var userConnections = userManager.GetConnectionsInGroups(new SignalRGroup(userId));
        var deviceConnections = GetDeviceConnections(deviceIds);

        return userConnections
            .Where(connection => !deviceConnections.Contains(connection))
            .ToList();
    }

    private IReadOnlyList<SignalRConnectionId> GetDeviceConnections(IEnumerable<Guid?> deviceIds)
    {
        return deviceIds.Distinct().SelectMany(GetDeviceConnections).ToList();
    }
    
    private IReadOnlyList<SignalRConnectionId> GetDeviceConnections(Guid? deviceId)
    {
        return deviceId.HasValue && deviceId.Value != Guid.Empty
            ? userManager.GetConnectionsInGroups(new SignalRGroup(deviceId.Value.ToString()))
            : new List<SignalRConnectionId>();
    }
}