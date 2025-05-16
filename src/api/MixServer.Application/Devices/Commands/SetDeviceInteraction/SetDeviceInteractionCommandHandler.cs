using MixServer.Domain.Users.Repositories;
using MixServer.Domain.Users.Services;
using MixServer.Infrastructure.Users.Repository;
using MixServer.Shared.Interfaces;

namespace MixServer.Application.Devices.Commands.SetDeviceInteraction;

public class SetDeviceInteractionCommandHandler(
    ICurrentDeviceRepository currentDeviceRepository,
    ICurrentUserRepository currentUserRepository,
    IDeviceTrackingService deviceTrackingService)
    : ICommandHandler<SetDeviceInteractionCommand>
{
    public Task HandleAsync(SetDeviceInteractionCommand request)
    {
        deviceTrackingService.SetInteraction(
            currentUserRepository.CurrentUserId,
            currentDeviceRepository.DeviceId,
            request.Interacted);

        return Task.CompletedTask;
    }
}