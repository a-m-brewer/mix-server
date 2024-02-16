using FluentValidation;
using MixServer.Application.Sessions.Responses;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Persistence;
using MixServer.Domain.Sessions.Entities;
using MixServer.Domain.Sessions.Services;
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
            ClientNotPlaying(request) ||
            CurrentPlaybackSessionsDiffer(request, serverSession) ||
            PlayingOnAnotherDevice(request, serverSession))
        {
            return new SyncPlaybackSessionResponse
            {
                UseClientState = false,
                Session = converter.Convert(serverSession, false)
            };
        }
        
        deviceTrackingService.SetInteraction(
            currentUserRepository.CurrentUserId,
            currentDeviceRepository.DeviceId,
            true);

        serverSession.DeviceId = currentDeviceRepository.DeviceId;
        serverSession.CurrentTime = TimeSpan.FromSeconds(request.CurrentTime);
        serverSession.Playing = true;

        playbackTrackingService.UpdateSessionStateIncludingPlaying(serverSession);

        await unitOfWork.SaveChangesAsync();

        return new SyncPlaybackSessionResponse
        {
            UseClientState = true,
            Session = converter.Convert(serverSession, true)
        };
    }

    private static bool ClientHasNoPlaybackSession(SyncPlaybackSessionCommand command) =>
        !command.PlaybackSessionId.HasValue;
    
    private static bool ClientNotPlaying(SyncPlaybackSessionCommand command) =>
        !command.Playing;

    private static bool CurrentPlaybackSessionsDiffer(SyncPlaybackSessionCommand command, PlaybackSession serverSession) =>
        serverSession.Id != command.PlaybackSessionId;

    private bool PlayingOnAnotherDevice(SyncPlaybackSessionCommand command, PlaybackSession serverSession)
    {
        if (!serverSession.SessionId.HasValue)
        {
            return false;
        }
        
        var currentDeviceId = currentDeviceRepository.DeviceId;

        return currentDeviceId != serverSession.SessionId.Value &&
               serverSession.Playing;
    }
}