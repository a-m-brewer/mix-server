using Microsoft.AspNetCore.SignalR;
using MixServer.Application.FileExplorer.Queries.GetNode;
using MixServer.Application.Queueing.Responses;
using MixServer.Application.Sessions.Converters;
using MixServer.Application.Sessions.Responses;
using MixServer.Application.Users.Dtos;
using MixServer.Application.Users.Responses;
using MixServer.Domain.Callbacks;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Queueing.Entities;
using MixServer.Domain.Sessions.Entities;
using MixServer.Domain.Sessions.Enums;
using MixServer.Domain.Sessions.Models;
using MixServer.Domain.Users.Entities;
using MixServer.Domain.Users.Enums;
using MixServer.Domain.Users.Models;
using MixServer.SignalR.Events;

namespace MixServer.SignalR;

public class SignalRCallbackService(
    IConverter<IDevice, DeviceDto> deviceDtoConverter,
    IConverter<IDeviceState, DeviceStateDto> deviceStateConverter,
    INodeResponseConverter nodeResponseConverter,
    IConverter<IPlaybackSession, bool, PlaybackSessionDto> playbackSessionConverter,
    IPlaybackStateConverter playbackStateConverter,
    IHubContext<SignalRCallbackHub, ISignalRCallbackClient> context,
    IConverter<QueueSnapshot, QueueSnapshotDto> queueSnapshotDtoConverter,
    IConverter<IUser, UserDto> userDtoConverter,
    ISignalRUserManager userManager)
    : ICallbackService
{
    public async Task CurrentSessionUpdated(string userId, PlaybackSession? session)
    {
        var currentSessionDto = session == null
            ? null
            : playbackSessionConverter.Convert(session, true);

        var clients = userManager.GetConnectionsInGroups(new SignalRGroup(userId));
        
        await context.Clients
            .Clients(clients)
            .CurrentSessionUpdated(new CurrentSessionUpdatedEventDto
            {
                CurrentPlaybackSession = currentSessionDto
            });
    }

    public async Task CurrentQueueUpdated(string userId, QueueSnapshot queueSnapshot)
    {
        var clients = userManager.GetConnectionsInGroups(new SignalRGroup(userId));

        var dto = queueSnapshotDtoConverter.Convert(queueSnapshot);

        await context.Clients
            .Clients(clients)
            .CurrentQueueUpdated(dto);
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

    public async Task FolderSorted(string userId, IFileExplorerFolderNode folder)
    {
        var clients = userManager.GetConnectionsInGroups(new SignalRGroup(userId));

        var dto = nodeResponseConverter.Convert(folder);

        await context.Clients
            .Clients(clients)
            .FolderSorted(dto);
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

        if (type == AudioPlayerStateUpdateType.Seek)
        {
            await context.Clients
                .Clients(deviceConnections)
                .PlaybackStateUpdated(dto);
        }
        
        await context.Clients
            .Clients(otherDevicesConnections)
            .PlaybackStateUpdated(dto);
    }

    public async Task PlaybackGranted(IPlaybackState state, bool useDeviceCurrentTime)
    {
        var (deviceConnections, otherDevicesConnections) =
            GetDevicesConnectionWithOtherDevices(state.UserId, state.DeviceId);
        
        var dto = playbackStateConverter.Convert(state, useDeviceCurrentTime);

        await context.Clients
            .Clients(otherDevicesConnections)
            .PlaybackStateUpdated(dto);
        
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

    public Task FileExplorerNodeAdded(IFileExplorerNode node)
    {
        return context.Clients
            .All
            .FileExplorerNodeAdded(nodeResponseConverter.Convert(node));
    }
    
    public Task FileExplorerNodeUpdated(IFileExplorerNode node, string oldAbsolutePath)
    {
        return context.Clients
            .All
            .FileExplorerNodeUpdated(new FileExplorerNodeUpdatedDto(nodeResponseConverter.Convert(node), oldAbsolutePath));
    }

    public Task FileExplorerNodeDeleted(IFileExplorerFolderNode parentNode, string absolutePath)
    {
        return context.Clients
            .All
            .FileExplorerNodeDeleted(new FileExplorerNodeDeletedDto(nodeResponseConverter.Convert(parentNode), absolutePath));
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
        var userConnections = userManager.GetConnectionsInGroups(new SignalRGroup(userId));
        var deviceConnections = deviceId.HasValue && deviceId.Value != Guid.Empty
            ? userManager.GetConnectionsInGroups(new SignalRGroup(deviceId.Value.ToString()))
            : new List<SignalRConnectionId>();

        var otherDevices = userConnections
            .Where(connection => !deviceConnections.Contains(connection))
            .ToList();

        return (deviceConnections, otherDevices);
    }
}