using MixServer.Domain.Users.Repositories;
using MixServer.Domain.Users.Services;
using MixServer.Infrastructure.Users.Repository;
using MixServer.Shared.Interfaces;

namespace MixServer.Application.Devices.Commands.UpdateDevicePlaybackCapabilities;

public class UpdateDevicePlaybackCapabilitiesCommandHandler(
    ICurrentUserRepository currentUserRepository,
    ICurrentDeviceRepository currentDeviceRepository,
    IDeviceTrackingService deviceTrackingService)
    : ICommandHandler<UpdateDevicePlaybackCapabilitiesCommand>
{
    public Task HandleAsync(UpdateDevicePlaybackCapabilitiesCommand request)
    {
        deviceTrackingService.UpdateCapabilities(
            currentUserRepository.CurrentUserId,
            currentDeviceRepository.DeviceId,
            request.Capabilities);
        
        return Task.CompletedTask;
    }
}
