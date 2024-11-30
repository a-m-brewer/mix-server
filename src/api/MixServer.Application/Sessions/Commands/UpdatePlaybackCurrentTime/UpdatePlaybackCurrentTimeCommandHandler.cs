using FluentValidation;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Sessions.Services;
using MixServer.Domain.Users.Repositories;
using MixServer.Infrastructure.Users.Repository;

namespace MixServer.Application.Sessions.Commands.UpdatePlaybackCurrentTime;

public class UpdatePlaybackCurrentTimeCommandHandler(
    ICurrentDeviceRepository currentDeviceRepository,
    ICurrentUserRepository currentUserRepository,
    IPlaybackTrackingService playbackTrackingService,
    IValidator<UpdatePlaybackCurrentTimeCommand> validator)
    : ICommandHandler<UpdatePlaybackCurrentTimeCommand>
{
    public async Task HandleAsync(UpdatePlaybackCurrentTimeCommand request)
    {
        await validator.ValidateAndThrowAsync(request);
        
        playbackTrackingService.UpdateAudioPlayerCurrentTime(
            currentUserRepository.CurrentUserId,
            currentDeviceRepository.DeviceId,
            request.CurrentTime);
    }
}