using MixServer.Domain.Interfaces;
using MixServer.Domain.Sessions.Services;
using MixServer.Domain.Users.Repositories;
using MixServer.Domain.Users.Services;
using MixServer.Infrastructure.Users.Repository;

namespace MixServer.Application.Devices.Commands.SetDeviceOnline;

public class SetDeviceOnlineCommandHandler(
    ICurrentUserRepository currentUserRepository,
    ICurrentDeviceRepository currentDeviceRepository,
    IDeviceTrackingService deviceTrackingService,
    IPlaybackTrackingService playbackTrackingService)
    : ICommandHandler<SetDeviceOnlineCommand>
{
    public Task HandleAsync(SetDeviceOnlineCommand request, CancellationToken cancellationToken = default)
    {
        deviceTrackingService.SetOnline(currentUserRepository.CurrentUserId, currentDeviceRepository.DeviceId, request.Online);
        
        if (!request.Online)
        {
            playbackTrackingService.HandleDeviceDisconnected(currentUserRepository.CurrentUserId, currentDeviceRepository.DeviceId);
        }
        
        return Task.CompletedTask;
    }
}