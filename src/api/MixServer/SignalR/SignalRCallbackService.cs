using Microsoft.AspNetCore.SignalR;
using MixServer.Application.Devices.Responses;
using MixServer.Application.FileExplorer.Queries.GetNode;
using MixServer.Application.Queueing.Responses;
using MixServer.Application.Sessions.Converters;
using MixServer.Application.Sessions.Responses;
using MixServer.Application.Users.Dtos;
using MixServer.Domain.Callbacks;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Queueing.Entities;
using MixServer.Domain.Sessions.Entities;
using MixServer.Domain.Sessions.Enums;
using MixServer.Domain.Sessions.Models;
using MixServer.Domain.Streams.Enums;
using MixServer.Domain.Users.Entities;
using MixServer.Domain.Users.Enums;
using MixServer.Domain.Users.Models;
using MixServer.SignalR.Events;

namespace MixServer.SignalR;

public class SignalRCallbackService(
    IConverter<IDevice, DeviceDto> deviceDtoConverter,
    IConverter<IDeviceState, DeviceStateDto> deviceStateConverter,
    IFileExplorerResponseConverter fileExplorerResponseConverter,
    IConverter<IPlaybackSession, bool, PlaybackSessionDto> playbackSessionConverter,
    IPlaybackStateConverter playbackStateConverter,
    IHubContext<SignalRCallbackHub, ISignalRCallbackClient> context,
    IConverter<QueueSnapshot, QueueSnapshotDto> queueSnapshotDtoConverter,
    IConverter<IUser, UserDto> userDtoConverter,
    ISignalRUserManager userManager)
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

    public async Task CurrentQueueUpdated(string userId, QueueSnapshot queueSnapshot)
    {
        await CurrentQueueUpdated(userManager.GetConnectionsInGroups(new SignalRGroup(userId)), queueSnapshot);
    }

    public Task CurrentQueueUpdated(string userId, Guid deviceId, QueueSnapshot queueSnapshot)
    {
        return CurrentQueueUpdated(GetDeviceConnectionsExcept(userId, deviceId), queueSnapshot);
    }
    
    private async Task CurrentQueueUpdated(IReadOnlyList<SignalRConnectionId> clients, QueueSnapshot queueSnapshot)
    {
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

    public async Task FolderSorted(string userId, IFileExplorerFolder folder)
    {
        var clients = userManager.GetConnectionsInGroups(new SignalRGroup(userId));

        var dto = fileExplorerResponseConverter.Convert(folder);

        await context.Clients
            .Clients(clients)
            .FolderSorted(dto);
    }

    public async Task FolderRefreshed(string userId, Guid deviceId, IFileExplorerFolder folder)
    {
        // Current Device is notified via RefreshFolderCommandHandler
        var (_, otherDevicesConnections) = GetDevicesConnectionWithOtherDevices(userId, deviceId);

        var dto = fileExplorerResponseConverter.Convert(folder);

        await context.Clients
            .Clients(otherDevicesConnections)
            .FolderRefreshed(dto);
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

    public Task FileExplorerNodeAdded(Dictionary<string, int> expectedNodeIndexes, IFileExplorerNode node)
    {
        var tasks = new List<Task>();
        var nodeDto = fileExplorerResponseConverter.Convert(node);
        foreach (var user in  userManager.ConnectedUsers)
        {
            var index = expectedNodeIndexes.TryGetValue(user.SignalRUserId.Id, out var i) ? i : -1;
            var dto = new FileExplorerNodeAddedDto
            {
                Node = nodeDto,
                Index = index
            };
            var connections = user.GetConnections();
            var task = context.Clients.Clients(connections).FileExplorerNodeAdded(dto);
            tasks.Add(task);
        }
        
        return Task.WhenAll(tasks);
    }
    
    public Task FileExplorerNodeUpdated(Dictionary<string, int> expectedNodeIndexes, IFileExplorerNode node, string oldAbsolutePath)
    {
        var tasks = new List<Task>();
        var nodeDto = fileExplorerResponseConverter.Convert(node);
        foreach (var user in  userManager.ConnectedUsers)
        {
            var index = expectedNodeIndexes.TryGetValue(user.SignalRUserId.Id, out var i) ? i : -1;
            var dto = new FileExplorerNodeUpdatedDto(nodeDto, oldAbsolutePath, index);
            var connections = user.GetConnections();
            var task = context.Clients.Clients(connections).FileExplorerNodeUpdated(dto);
            tasks.Add(task);
        }
        
        return Task.WhenAll(tasks);
    }

    public Task FileExplorerNodeDeleted(IFileExplorerFolderNode parentNode, string absolutePath)
    {
        return context.Clients
            .All
            .FileExplorerNodeDeleted(new FileExplorerNodeDeletedDto(fileExplorerResponseConverter.Convert(parentNode), absolutePath));
    }

    public Task TranscodeStatusUpdated(string hash, TranscodeState state)
    {
        return context.Clients
            .All
            .TranscodeStatusUpdated(new TranscodeStatusUpdatedDto
            {
                FileHash = hash,
                TranscodeState = state
            });
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