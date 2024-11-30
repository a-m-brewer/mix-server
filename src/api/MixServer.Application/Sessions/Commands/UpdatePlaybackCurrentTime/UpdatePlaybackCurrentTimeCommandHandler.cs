using FluentValidation;
using Microsoft.Extensions.Logging;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Sessions.Services;
using MixServer.Domain.Users.Repositories;
using MixServer.Infrastructure.Users.Repository;

namespace MixServer.Application.Sessions.Commands.UpdatePlaybackState;

public class UpdatePlaybackStateCommandHandler(
    ICurrentDeviceRepository currentDeviceRepository,
    ICurrentUserRepository currentUserRepository,
    IPlaybackTrackingService playbackTrackingService,
    IValidator<UpdatePlaybackStateCommand> validator)
    : ICommandHandler<UpdatePlaybackStateCommand>
{
    public async Task HandleAsync(UpdatePlaybackStateCommand request)
    {
        await validator.ValidateAndThrowAsync(request);
        
        playbackTrackingService.UpdateAudioPlayerCurrentTime(
            currentUserRepository.CurrentUserId,
            currentDeviceRepository.DeviceId,
            request.CurrentTime);
    }
}