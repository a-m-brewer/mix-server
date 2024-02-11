using MixServer.Domain.Interfaces;
using MixServer.Domain.Users.Repositories;
using MixServer.Domain.Users.Services;
using MixServer.Infrastructure.Users.Repository;

namespace MixServer.Application.Users.Commands.SetDeviceInteraction;

public class SetDeviceInteractionCommandHandler : ICommandHandler<SetDeviceInteractionCommand>
{
    private readonly ICurrentDeviceRepository _currentDeviceRepository;
    private readonly ICurrentUserRepository _currentUserRepository;
    private readonly IDeviceTrackingService _deviceTrackingService;

    public SetDeviceInteractionCommandHandler(
        ICurrentDeviceRepository currentDeviceRepository,
        ICurrentUserRepository currentUserRepository,
        IDeviceTrackingService deviceTrackingService)
    {
        _currentDeviceRepository = currentDeviceRepository;
        _currentUserRepository = currentUserRepository;
        _deviceTrackingService = deviceTrackingService;
    }
    
    public Task HandleAsync(SetDeviceInteractionCommand request)
    {
        _deviceTrackingService.SetInteraction(
            _currentUserRepository.CurrentUserId,
            _currentDeviceRepository.DeviceId,
            request.Interacted);

        return Task.CompletedTask;
    }
}