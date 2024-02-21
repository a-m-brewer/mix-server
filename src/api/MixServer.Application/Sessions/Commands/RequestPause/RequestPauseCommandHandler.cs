using Microsoft.Extensions.Logging;
using MixServer.Domain.Callbacks;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Sessions.Services;
using MixServer.Infrastructure.Users.Repository;

namespace MixServer.Application.Sessions.Commands.RequestPause;

public class RequestPauseCommandHandler(
    ICallbackService callbackService,
    ICurrentUserRepository currentUserRepository,
    ILogger<RequestPauseCommandHandler> logger,
    IPlaybackTrackingService playbackTrackingService)
    : ICommandHandler<RequestPauseCommand>
{
    public async Task HandleAsync(RequestPauseCommand request)
    {
        var state = playbackTrackingService.GetOrThrow(currentUserRepository.CurrentUserId);
        
        logger.LogInformation("Sending request to pause to: {DeviceId}", state.DeviceId);
        playbackTrackingService.SetWaitingForPause(currentUserRepository.CurrentUserId);
        
        await callbackService.PauseRequested(state.DeviceIdOrThrow);
    }
}