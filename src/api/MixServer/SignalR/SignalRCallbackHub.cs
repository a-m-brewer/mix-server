using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.SignalR;
using MixServer.Application.Devices.Commands.SetDeviceInteraction;
using MixServer.Application.Devices.Commands.SetDeviceOnline;
using MixServer.Application.Sessions.Commands.UpdatePlaybackState;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Users.Repositories;
using MixServer.Infrastructure.Users.Repository;
using MixServer.SignalR.Commands;
using MixServer.SignalR.Events;

namespace MixServer.SignalR;

public class SignalRCallbackHub(
    ICurrentDeviceRepository currentDeviceRepository,
    ICurrentUserRepository currentUserRepository,
    ICommandHandler<UpdatePlaybackStateCommand> updatePlaybackStateCommandHandler,
    ICommandHandler<SetDeviceOnlineCommand> setDeviceOnlineCommandHandler,
    ICommandHandler<SetDeviceInteractionCommand> setDeviceInteractionCommandHandler,
    ILogger<SignalRCallbackHub> logger,
    ISignalRUserManager signalRUserManager)
    : Hub<ISignalRCallbackClient>
{
    public override async Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();
        if (httpContext != null && Context.User != null)
        {
            var accessToken = await httpContext.GetTokenAsync("access_token");

            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                await signalRUserManager.UserConnectedAsync(
                    Context.User,
                    new SignalRConnectionId(Context.ConnectionId),
                    accessToken);
                await SetDeviceOnline(true);
            }
        }
        
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (Context.User == null)
        {
            await base.OnDisconnectedAsync(exception);
            return;
        }

        await SetDeviceOnline(false);

        var connectionId = new SignalRConnectionId(Context.ConnectionId);
        signalRUserManager.UserDisconnected(Context.User, connectionId);

        await base.OnDisconnectedAsync(exception);
    }

    public async Task UpdatePlaybackState(SignalRUpdatePlaybackStateCommand command)
    {
        try
        {
            await updatePlaybackStateCommandHandler.HandleAsync(new UpdatePlaybackStateCommand
            {
                CurrentTime = TimeSpan.FromSeconds(command.CurrentTime)
            });
        }
        catch (Exception e)
        {
            logger.LogError(e, "Un-expected Exception");
        }
    }

    public async Task PageClosed()
    {
        await setDeviceInteractionCommandHandler.HandleAsync(new SetDeviceInteractionCommand
        {
            Interacted = false
        });
    }

    public void Log(DebugMessageDto message)
    {
        logger.Log(message.Level, "[User: {UserId} Device: {DeviceId}]: {Message}",
            currentUserRepository.CurrentUserId,
            currentDeviceRepository.DeviceId,
            message.Message);
    }

    private async Task SetDeviceOnline(bool online)
    {
        try
        {
            await setDeviceOnlineCommandHandler.HandleAsync(new SetDeviceOnlineCommand(online));
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to set device online status to: {Online}", online);
        }
    }
}