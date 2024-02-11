using Microsoft.Extensions.Logging;
using MixServer.Domain.Callbacks;
using MixServer.Domain.Exceptions;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Sessions.Models;
using MixServer.Domain.Sessions.Services;
using MixServer.Domain.Users.Services;
using MixServer.Infrastructure.Users.Repository;

namespace MixServer.Application.Sessions.Commands.RequestPlayback;

public class RequestPlaybackCommandHandler : ICommandHandler<RequestPlaybackCommand>
{
    private readonly ICallbackService _callbackService;
    private readonly IConnectionManager _connectionManager;
    private readonly ICurrentUserRepository _currentUserRepository;
    private readonly IDeviceTrackingService _deviceTrackingService;
    private readonly ILogger<RequestPlaybackCommandHandler> _logger;
    private readonly IPlaybackTrackingService _playbackTrackingService;

    public RequestPlaybackCommandHandler(
        ICallbackService callbackService,
        IConnectionManager connectionManager,
        ICurrentUserRepository currentUserRepository,
        IDeviceTrackingService deviceTrackingService,
        ILogger<RequestPlaybackCommandHandler> logger,
        IPlaybackTrackingService playbackTrackingService)
    {
        _callbackService = callbackService;
        _connectionManager = connectionManager;
        _currentUserRepository = currentUserRepository;
        _deviceTrackingService = deviceTrackingService;
        _logger = logger;
        _playbackTrackingService = playbackTrackingService;
    }
    
    public async Task HandleAsync(RequestPlaybackCommand request)
    {
        var playbackState = _playbackTrackingService.GetOrThrow(_currentUserRepository.CurrentUserId);

        if (!playbackState.HasDevice)
        {
            _logger.LogInformation("Current session is not currently playing on any device. Granting Playback to: {DeviceId}", request.DeviceId);
            await UpdatePlaybackDeviceAsync(null, request.DeviceId);
            return;
        }

        if (playbackState.DeviceId == request.DeviceId)
        {
            _logger.LogInformation("Requesting playback device: {DeviceId} is current playback device. Starting Playback",
                playbackState.DeviceId);
            await SendPlaybackGrantedAsync(playbackState, true);
            return;
        }

        if (!_deviceTrackingService.DeviceInteractedWith(request.DeviceId))
        {
            throw new InvalidRequestException(nameof(RequestPlaybackCommand.DeviceId),
                "Can not request playback with a device that has not been interacted with due to browser auto play rules");
        }

        if (!_connectionManager.DeviceConnected(playbackState.DeviceIdOrThrow))
        {
            await UpdatePlaybackDeviceAsync(playbackState.DeviceIdOrThrow, request.DeviceId);
            return;
        }

        _playbackTrackingService.SetWaitingForPause(_currentUserRepository.CurrentUserId);
        
        _logger.LogInformation("Sending request to pause to: {DeviceId}", playbackState.DeviceId);
        await _callbackService.PauseRequested(playbackState.DeviceIdOrThrow);

        _playbackTrackingService.WaitForPause(_currentUserRepository.CurrentUserId);
        
        playbackState = _playbackTrackingService.GetOrThrow(_currentUserRepository.CurrentUserId);

        await UpdatePlaybackDeviceAsync(playbackState.DeviceIdOrThrow, request.DeviceId);
    }

    private async Task UpdatePlaybackDeviceAsync(Guid? currentDeviceId, Guid requestedPlaybackDevice)
    {
        _logger.LogInformation("Current playback device: {CurrentPlaybackDevice} transferring playback to: {NextPlaybackDevice}",
            currentDeviceId,
            requestedPlaybackDevice);
            
        _playbackTrackingService.UpdatePlaybackDevice(_currentUserRepository.CurrentUserId, requestedPlaybackDevice);

        var newPlaybackState = _playbackTrackingService.GetOrThrow(_currentUserRepository.CurrentUserId);

        await SendPlaybackGrantedAsync(newPlaybackState, false);
    }

    private async Task SendPlaybackGrantedAsync(IPlaybackState state, bool useDeviceCurrentTime)
    {
        await _callbackService.PlaybackGranted(state, useDeviceCurrentTime);
    }
}