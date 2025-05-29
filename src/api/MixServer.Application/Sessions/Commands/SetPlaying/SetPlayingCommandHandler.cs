using MixServer.Domain.Interfaces;
using MixServer.Domain.Sessions.Services;
using MixServer.Domain.Users.Repositories;
using MixServer.Infrastructure.Users.Repository;

namespace MixServer.Application.Sessions.Commands.SetPlaying;

public class SetPlayingCommandHandler(
    ICurrentUserRepository currentUserRepository,
    IPlaybackTrackingService playbackTrackingService)
    : ICommandHandler<SetPlayingCommand>
{
    public Task HandleAsync(SetPlayingCommand request, CancellationToken cancellationToken = default)
    {
        playbackTrackingService.SetPlaying(
            currentUserRepository.CurrentUserId, 
            request.Playing,
            TimeSpan.FromSeconds(request.CurrentTime));
        
        return Task.CompletedTask;
    }
}