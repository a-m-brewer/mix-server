using MixServer.Application.Users.Commands.SetDeviceInteraction;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Sessions.Services;
using MixServer.Domain.Users.Repositories;
using MixServer.Infrastructure.Users.Repository;

namespace MixServer.Application.Users.Commands.SetDeviceDisconnected;

public class SetDeviceDisconnectedCommandHandler : ICommandHandler<SetDeviceDisconnectedCommand>
{
    private readonly ICurrentUserRepository _currentUserRepository;
    private readonly ICurrentDeviceRepository _currentDeviceRepository;
    private readonly IPlaybackTrackingService _playbackTrackingService;
    private readonly ICommandHandler<SetDeviceInteractionCommand> _setDeviceInteractionCommandHandler;

    public SetDeviceDisconnectedCommandHandler(
        ICurrentUserRepository currentUserRepository,
        ICurrentDeviceRepository currentDeviceRepository,
        IPlaybackTrackingService playbackTrackingService,
        ICommandHandler<SetDeviceInteractionCommand> setDeviceInteractionCommandHandler)
    {
        _currentUserRepository = currentUserRepository;
        _currentDeviceRepository = currentDeviceRepository;
        _playbackTrackingService = playbackTrackingService;
        _setDeviceInteractionCommandHandler = setDeviceInteractionCommandHandler;
    }
    
    public async Task HandleAsync(SetDeviceDisconnectedCommand request)
    {
        await _setDeviceInteractionCommandHandler.HandleAsync(new SetDeviceInteractionCommand { Interacted = false });
        _playbackTrackingService.HandleDeviceDisconnected(_currentUserRepository.CurrentUserId, _currentDeviceRepository.DeviceId);
    }
}