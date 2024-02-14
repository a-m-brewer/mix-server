using Microsoft.Extensions.Logging;
using MixServer.Domain.Callbacks;
using MixServer.Domain.Exceptions;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Sessions.Models;
using MixServer.Domain.Sessions.Services;
using MixServer.Domain.Users.Services;
using MixServer.Infrastructure.Users.Repository;

namespace MixServer.Application.Sessions.Commands.RequestPlayback;

public class RequestPlaybackCommandHandler(
    ICallbackService callbackService,
    IConnectionManager connectionManager,
    ICurrentUserRepository currentUserRepository,
    IDeviceTrackingService deviceTrackingService,
    ILogger<RequestPlaybackCommandHandler> logger,
    IPlaybackTrackingService playbackTrackingService)
    : ICommandHandler<RequestPlaybackCommand>
{
    public async Task HandleAsync(RequestPlaybackCommand request)
    {
        var playbackState = playbackTrackingService.GetOrThrow(currentUserRepository.CurrentUserId);

        if (!playbackState.HasDevice)
        {
            logger.LogInformation("Current session is not currently playing on any device. Granting Playback to: {DeviceId}", request.DeviceId);
            await UpdatePlaybackDeviceAsync(null, request.DeviceId);
            return;
        }

        if (playbackState.DeviceId == request.DeviceId)
        {
            logger.LogInformation("Requesting playback device: {DeviceId} is current playback device. Starting Playback",
                playbackState.DeviceId);
            await SendPlaybackGrantedAsync(playbackState, true);
            return;
        }

        if (!deviceTrackingService.DeviceInteractedWith(request.DeviceId))
        {
            throw new InvalidRequestException(nameof(RequestPlaybackCommand.DeviceId),
                "Can not request playback with a device that has not been interacted with due to browser auto play rules");
        }

        if (!connectionManager.DeviceConnected(playbackState.DeviceIdOrThrow))
        {
            await UpdatePlaybackDeviceAsync(playbackState.DeviceIdOrThrow, request.DeviceId);
            return;
        }

        playbackTrackingService.SetWaitingForPause(currentUserRepository.CurrentUserId);
        
        logger.LogInformation("Sending request to pause to: {DeviceId}", playbackState.DeviceId);
        await callbackService.PauseRequested(playbackState.DeviceIdOrThrow);

        playbackTrackingService.WaitForPause(currentUserRepository.CurrentUserId);
        
        playbackState = playbackTrackingService.GetOrThrow(currentUserRepository.CurrentUserId);

        await UpdatePlaybackDeviceAsync(playbackState.DeviceIdOrThrow, request.DeviceId);
    }

    private async Task UpdatePlaybackDeviceAsync(Guid? currentDeviceId, Guid requestedPlaybackDevice)
    {
        logger.LogInformation("Current playback device: {CurrentPlaybackDevice} transferring playback to: {NextPlaybackDevice}",
            currentDeviceId,
            requestedPlaybackDevice);
            
        playbackTrackingService.UpdatePlaybackDevice(currentUserRepository.CurrentUserId, requestedPlaybackDevice);

        var newPlaybackState = playbackTrackingService.GetOrThrow(currentUserRepository.CurrentUserId);

        await SendPlaybackGrantedAsync(newPlaybackState, false);
    }

    private async Task SendPlaybackGrantedAsync(IPlaybackState state, bool useDeviceCurrentTime)
    {
        await callbackService.PlaybackGranted(state, useDeviceCurrentTime);
    }
}