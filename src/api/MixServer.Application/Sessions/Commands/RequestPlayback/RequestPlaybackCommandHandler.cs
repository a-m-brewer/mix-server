using Microsoft.Extensions.Logging;
using MixServer.Application.Sessions.Converters;
using MixServer.Application.Sessions.Dtos;
using MixServer.Domain.Callbacks;
using MixServer.Domain.Exceptions;
using MixServer.Domain.FileExplorer.Services.Caching;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Sessions.Models;
using MixServer.Domain.Sessions.Services;
using MixServer.Domain.Sessions.Validators;
using MixServer.Domain.Users.Repositories;
using MixServer.Domain.Users.Services;
using MixServer.Infrastructure.Users.Repository;

namespace MixServer.Application.Sessions.Commands.RequestPlayback;

public class RequestPlaybackCommandHandler(
    IPlaybackStateConverter converter,
    ICallbackService callbackService,
    ICanPlayOnDeviceValidator canPlayOnDeviceValidator,
    IConnectionManager connectionManager,
    ICurrentDeviceRepository currentDeviceRepository,
    ICurrentUserRepository currentUserRepository,
    IDeviceTrackingService deviceTrackingService,
    IFolderCacheService folderCacheService,
    ILogger<RequestPlaybackCommandHandler> logger,
    IPlaybackTrackingService playbackTrackingService)
    : ICommandHandler<RequestPlaybackCommand, PlaybackGrantedDto>
{
    public async Task<PlaybackGrantedDto> HandleAsync(RequestPlaybackCommand request)
    {
        var playbackState = playbackTrackingService.GetOrThrow(currentUserRepository.CurrentUserId);
        
        var deviceState = deviceTrackingService.GetDeviceStateOrThrow(request.DeviceId);
        var file = folderCacheService.GetFile(playbackState.AbsolutePath);

        canPlayOnDeviceValidator.ValidateCanPlayOrThrow(deviceState, file);

        if (!playbackState.HasDevice)
        {
            logger.LogInformation("Current session is not currently playing on any device. Granting Playback to: {DeviceId}", request.DeviceId);
            return await UpdatePlaybackDeviceAsync(null, request.DeviceId);
        }

        if (playbackState.DeviceId == request.DeviceId)
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
            return await UpdatePlaybackDeviceAsync(playbackState.DeviceIdOrThrow, request.DeviceId);
        }

        playbackTrackingService.SetWaitingForPause(currentUserRepository.CurrentUserId);
        
        logger.LogInformation("Sending request to pause to: {DeviceId}", playbackState.DeviceId);
        await callbackService.PauseRequested(playbackState.DeviceIdOrThrow);

        playbackTrackingService.WaitForPause(currentUserRepository.CurrentUserId);
        
        playbackState = playbackTrackingService.GetOrThrow(currentUserRepository.CurrentUserId);

        return await UpdatePlaybackDeviceAsync(playbackState.DeviceIdOrThrow, request.DeviceId);
    }

    private async Task<PlaybackGrantedDto> UpdatePlaybackDeviceAsync(Guid? currentDeviceId, Guid requestedPlaybackDevice)
    {
        logger.LogInformation("Current playback device: {CurrentPlaybackDevice} transferring playback to: {NextPlaybackDevice}",
            currentDeviceId,
            requestedPlaybackDevice);
            
        playbackTrackingService.UpdatePlaybackDevice(currentUserRepository.CurrentUserId, requestedPlaybackDevice);

        var newPlaybackState = playbackTrackingService.GetOrThrow(currentUserRepository.CurrentUserId);

        return await SendPlaybackGrantedAsync(newPlaybackState, false);
    }

    private async Task<PlaybackGrantedDto> SendPlaybackGrantedAsync(IPlaybackState state, bool useDeviceCurrentTime)
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