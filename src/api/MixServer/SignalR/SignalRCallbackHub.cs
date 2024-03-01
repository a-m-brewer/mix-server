using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.SignalR;
using MixServer.Application.Sessions.Commands.UpdatePlaybackState;
using MixServer.Application.Users.Commands.SetDeviceDisconnected;
using MixServer.Application.Users.Commands.SetDeviceInteraction;
using MixServer.Domain.Interfaces;
using MixServer.SignalR.Commands;

namespace MixServer.SignalR;

public class SignalRCallbackHub(
    ICommandHandler<UpdatePlaybackStateCommand> updatePlaybackStateCommandHandler,
    ICommandHandler<SetDeviceDisconnectedCommand> setDeviceDisconnectedCommandHandler,
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

        try
        {
            await setDeviceDisconnectedCommandHandler.HandleAsync(new SetDeviceDisconnectedCommand());
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to set device disconnected");
        }

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
}