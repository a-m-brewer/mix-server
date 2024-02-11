using MixServer.Domain.Interfaces;
using MixServer.Domain.Sessions.Services;
using MixServer.Domain.Users.Repositories;
using MixServer.Infrastructure.Users.Repository;

namespace MixServer.Application.Sessions.Commands.SetPlaying;

public class SetPlayingCommandHandler : ICommandHandler<SetPlayingCommand>
{
    private readonly ICurrentUserRepository _currentUserRepository;
    private readonly ICurrentDeviceRepository _currentDeviceRepository;
    private readonly IPlaybackTrackingService _playbackTrackingService;

    public SetPlayingCommandHandler(
        ICurrentUserRepository currentUserRepository,
        ICurrentDeviceRepository currentDeviceRepository,
        IPlaybackTrackingService playbackTrackingService)
    {
        _currentUserRepository = currentUserRepository;
        _currentDeviceRepository = currentDeviceRepository;
        _playbackTrackingService = playbackTrackingService;
    }
    
    public Task HandleAsync(SetPlayingCommand request)
    {
        _playbackTrackingService.SetPlaying(
            _currentUserRepository.CurrentUserId, 
            request.Playing,
            TimeSpan.FromSeconds(request.CurrentTime));
        
        return Task.CompletedTask;
    }
}