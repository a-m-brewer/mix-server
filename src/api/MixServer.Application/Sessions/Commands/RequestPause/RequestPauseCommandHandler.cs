using Microsoft.Extensions.Logging;
using MixServer.Domain.Callbacks;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Sessions.Accessors;

namespace MixServer.Application.Sessions.Commands.RequestPause;

public class RequestPauseCommandHandler(
    ICallbackService callbackService,
    ILogger<RequestPauseCommandHandler> logger,
    IPlaybackTrackingAccessor playbackTrackingAccessor)
    : ICommandHandler<RequestPauseCommand>
{
    public async Task HandleAsync(RequestPauseCommand request, CancellationToken cancellationToken = default)
    {
        var state = await playbackTrackingAccessor.GetPlaybackStateAsync(cancellationToken);
        
        logger.LogInformation("Sending request to pause to: {DeviceId}", state.DeviceId);
        
        state.SetWaitingForPause();
        
        await callbackService.PauseRequested(state.DeviceIdOrThrow);
    }
}