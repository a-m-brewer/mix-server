using Microsoft.Extensions.Logging;
using MixServer.Domain.Callbacks;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Sessions.Services;
using MixServer.Infrastructure.Users.Repository;

namespace MixServer.Application.Sessions.Commands.RequestPause;

public class RequestPauseCommandHandler : ICommandHandler<RequestPauseCommand>
{
    private readonly ICallbackService _callbackService;
    private readonly ICurrentUserRepository _currentUserRepository;
    private readonly ILogger<RequestPauseCommandHandler> _logger;
    private readonly IPlaybackTrackingService _playbackTrackingService;

    public RequestPauseCommandHandler(
        ICallbackService callbackService,
        ICurrentUserRepository currentUserRepository,
        ILogger<RequestPauseCommandHandler> logger,
        IPlaybackTrackingService playbackTrackingService)
    {
        _callbackService = callbackService;
        _currentUserRepository = currentUserRepository;
        _logger = logger;
        _playbackTrackingService = playbackTrackingService;
    }
    
    public async Task HandleAsync(RequestPauseCommand request)
    {
        var state = _playbackTrackingService.GetOrThrow(_currentUserRepository.CurrentUserId);
        
        _logger.LogInformation("Sending request to pause to: {DeviceId}", state.DeviceId);
        _playbackTrackingService.SetWaitingForPause(_currentUserRepository.CurrentUserId);
        
        await _callbackService.PauseRequested(state.DeviceIdOrThrow);
    }
}