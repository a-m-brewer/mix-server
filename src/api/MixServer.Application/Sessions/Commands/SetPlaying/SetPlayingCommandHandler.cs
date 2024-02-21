using MixServer.Domain.Interfaces;
using MixServer.Domain.Sessions.Services;
using MixServer.Domain.Users.Repositories;
using MixServer.Infrastructure.Users.Repository;

namespace MixServer.Application.Sessions.Commands.SetPlaying;

public class SetPlayingCommandHandler(
    ICurrentUserRepository currentUserRepository,
    ICurrentDeviceRepository currentDeviceRepository,
    IPlaybackTrackingService playbackTrackingService)
    : ICommandHandler<SetPlayingCommand>
{
    private readonly ICurrentDeviceRepository _currentDeviceRepository = currentDeviceRepository;

    public Task HandleAsync(SetPlayingCommand request)
    {
        playbackTrackingService.SetPlaying(
            currentUserRepository.CurrentUserId, 
            request.Playing,
            TimeSpan.FromSeconds(request.CurrentTime));
        
        return Task.CompletedTask;
    }
}