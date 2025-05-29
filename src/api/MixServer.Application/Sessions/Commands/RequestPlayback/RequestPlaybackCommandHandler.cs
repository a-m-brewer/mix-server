using Microsoft.Extensions.Logging;
using MixServer.Application.Sessions.Converters;
using MixServer.Application.Sessions.Dtos;
using MixServer.Domain.Callbacks;
using MixServer.Domain.Exceptions;
using MixServer.Domain.FileExplorer.Services.Caching;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Sessions.Accessors;
using MixServer.Domain.Sessions.Models;
using MixServer.Domain.Sessions.Validators;
using MixServer.Domain.Users.Repositories;
using MixServer.Domain.Users.Services;

namespace MixServer.Application.Sessions.Commands.RequestPlayback;

public class RequestPlaybackCommandHandler(
    IPlaybackStateConverter converter,
    ICallbackService callbackService,
    ICanPlayOnDeviceValidator canPlayOnDeviceValidator,
    IConnectionManager connectionManager,
    ICurrentDeviceRepository currentDeviceRepository,
    IDeviceTrackingService deviceTrackingService,
    IFolderCacheService folderCacheService,
    ILogger<RequestPlaybackCommandHandler> logger,
    IPlaybackTrackingAccessor playbackTrackingAccessor)
    : ICommandHandler<RequestPlaybackCommand, PlaybackGrantedDto>
{
    public async Task<PlaybackGrantedDto> HandleAsync(RequestPlaybackCommand request, CancellationToken cancellationToken = default)
    {
        var playbackState = await playbackTrackingAccessor.GetPlaybackStateAsync(cancellationToken);

        var requestDeviceId = request.DeviceId ?? currentDeviceRepository.DeviceId;
        
        var deviceState = deviceTrackingService.GetDeviceStateOrThrow(requestDeviceId);

        if (playbackState.NodePath is null)
        {
            throw new InvalidRequestException(nameof(playbackState.NodePath), "Playback file state is not set");
        }

        var file = await folderCacheService.GetFileAsync(playbackState.NodePath);

        canPlayOnDeviceValidator.ValidateCanPlayOrThrow(deviceState, file);

        if (!playbackState.HasDevice)
        {
            logger.LogInformation("Current session is not currently playing on any device. Granting Playback to: {DeviceId}", requestDeviceId);
            return await UpdatePlaybackDeviceAsync(playbackState, requestDeviceId);
        }

        if (playbackState.DeviceId == requestDeviceId)
        {
            logger.LogInformation("Requesting playback device: {DeviceId} is current playback device. Starting Playback",
                playbackState.DeviceId);
            return await SendPlaybackGrantedAsync(playbackState, true);
        }
        
        if (!deviceState.InteractedWith)
        {
            throw new InvalidRequestException(nameof(RequestPlaybackCommand.DeviceId),
                "Can not request playback with a device that has not been interacted with due to browser auto play rules");
        }

        if (!connectionManager.DeviceConnected(playbackState.DeviceIdOrThrow))
        {
            return await UpdatePlaybackDeviceAsync(playbackState, requestDeviceId);
        }

        playbackState.SetWaitingForPause();
        
        logger.LogInformation("Sending request to pause to: {DeviceId}", playbackState.DeviceId);
        await callbackService.PauseRequested(playbackState.DeviceIdOrThrow);

        playbackState.WaitForPause();

        return await UpdatePlaybackDeviceAsync(playbackState, requestDeviceId);
    }

    private async Task<PlaybackGrantedDto> UpdatePlaybackDeviceAsync(PlaybackState state, Guid requestedPlaybackDevice)
    {
        logger.LogInformation("Current playback device: {CurrentPlaybackDevice} transferring playback to: {NextPlaybackDevice}",
            state.DeviceId,
            requestedPlaybackDevice);

        state.DeviceId = requestedPlaybackDevice;

        return await SendPlaybackGrantedAsync(state, false);
    }

    private async Task<PlaybackGrantedDto> SendPlaybackGrantedAsync(PlaybackState state, bool useDeviceCurrentTime)
    {
        var currentDeviceId = currentDeviceRepository.DeviceId;
        
        // update other devices that are not the currentDevice or the new playback device
        await callbackService.PlaybackStateUpdated(state, currentDeviceId, useDeviceCurrentTime);
        
        // If the requester is not the one getting playback permission we need to send the playback granted event to that device
        if (currentDeviceId != state.DeviceId)
        {
            await callbackService.PlaybackGranted(state, useDeviceCurrentTime);
        }

        return converter.Convert(state, useDeviceCurrentTime);
    }
}