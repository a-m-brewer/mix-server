using MixServer.Application.Users.Commands.SetDeviceInteraction;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Sessions.Services;
using MixServer.Domain.Users.Repositories;
using MixServer.Infrastructure.Users.Repository;

namespace MixServer.Application.Users.Commands.SetDeviceDisconnected;

public class SetDeviceDisconnectedCommandHandler(
    ICurrentUserRepository currentUserRepository,
    ICurrentDeviceRepository currentDeviceRepository,
    IPlaybackTrackingService playbackTrackingService,
    ICommandHandler<SetDeviceInteractionCommand> setDeviceInteractionCommandHandler)
    : ICommandHandler<SetDeviceDisconnectedCommand>
{
    public async Task HandleAsync(SetDeviceDisconnectedCommand request)
    {
        await setDeviceInteractionCommandHandler.HandleAsync(new SetDeviceInteractionCommand { Interacted = false });
        playbackTrackingService.HandleDeviceDisconnected(currentUserRepository.CurrentUserId, currentDeviceRepository.DeviceId);
    }
}