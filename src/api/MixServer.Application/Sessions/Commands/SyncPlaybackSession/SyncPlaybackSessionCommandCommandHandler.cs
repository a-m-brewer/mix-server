using FluentValidation;
using MixServer.Application.Sessions.Responses;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Persistence;
using MixServer.Domain.Sessions.Entities;
using MixServer.Domain.Sessions.Models;
using MixServer.Domain.Sessions.Services;
using MixServer.Domain.Tracklists.Builders;
using MixServer.Domain.Tracklists.Factories;
using MixServer.Domain.Users.Repositories;
using MixServer.Domain.Users.Services;
using MixServer.Infrastructure.Users.Repository;

namespace MixServer.Application.Sessions.Commands.SyncPlaybackSession;

public class SyncPlaybackSessionCommandCommandHandler(
    IConverter<IPlaybackSession, bool, PlaybackSessionDto> converter,
    ICurrentUserRepository currentUserRepository,
    ICurrentDeviceRepository currentDeviceRepository,
    IDeviceTrackingService deviceTrackingService,
    IPlaybackTrackingService playbackTrackingService,
    ISessionService sessionService,
    IValidator<SyncPlaybackSessionCommand> validator,
    IUnitOfWork unitOfWork)
    : ICommandHandler<SyncPlaybackSessionCommand, SyncPlaybackSessionResponse>
{
    public async Task<SyncPlaybackSessionResponse> HandleAsync(SyncPlaybackSessionCommand request)
    {
        await validator.ValidateAndThrowAsync(request);
        
        var serverSession = await sessionService.GetCurrentPlaybackSessionWithFileAsync();

        if (serverSession.File is { Parent.BelongsToRootChild: false })
        {
            sessionService.ClearUsersCurrentSession();
            await unitOfWork.SaveChangesAsync();

            return new SyncPlaybackSessionResponse
            {
                UseClientState = false,
                Session = null
            };
        }
        
        if (ClientHasNoPlaybackSession(request) ||
            CurrentPlaybackSessionsDiffer(request, serverSession) ||
            CurrentlyPlayingOnAnotherDevice(serverSession))
        {
            return new SyncPlaybackSessionResponse
            {
                UseClientState = false,
                Session = converter.Convert(serverSession, false)
            };
        }
        
        if (request.Playing)
        {
            deviceTrackingService.SetInteraction(
                currentUserRepository.CurrentUserId,
                currentDeviceRepository.DeviceId,
                true);

            serverSession.DeviceId = currentDeviceRepository.DeviceId;
        }

        if (request.Playing || !PlayedOnAnotherDeviceSinceLastSync(serverSession))
        {
            serverSession.CurrentTime = TimeSpan.FromSeconds(request.CurrentTime);
        }
        
        serverSession.Playing = request.Playing;

        playbackTrackingService.UpdateSessionStateIncludingPlaying(serverSession);

        await unitOfWork.SaveChangesAsync();

        return new SyncPlaybackSessionResponse
        {
            UseClientState = request.Playing,
            Session = converter.Convert(serverSession, true)
        };
    }

    private static bool ClientHasNoPlaybackSession(SyncPlaybackSessionCommand command) =>
        !command.PlaybackSessionId.HasValue;

    private static bool CurrentPlaybackSessionsDiffer(SyncPlaybackSessionCommand command, PlaybackSession serverSession) =>
        serverSession.Id != command.PlaybackSessionId;

    private bool PlayedOnAnotherDeviceSinceLastSync(IPlaybackState serverSession) => 
        serverSession.LastPlaybackDeviceId != currentDeviceRepository.DeviceId;

    private bool CurrentlyPlayingOnAnotherDevice(PlaybackSession serverSession) =>
        serverSession.DeviceId != currentDeviceRepository.DeviceId && serverSession.Playing;
}