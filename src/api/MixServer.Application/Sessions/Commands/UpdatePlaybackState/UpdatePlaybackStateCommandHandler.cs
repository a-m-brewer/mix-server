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
    ILogger<UpdatePlaybackStateCommandHandler> logger,
    IPlaybackTrackingService playbackTrackingService,
    IValidator<UpdatePlaybackStateCommand> validator)
    : ICommandHandler<UpdatePlaybackStateCommand>
{
    private readonly ILogger<UpdatePlaybackStateCommandHandler> _logger = logger;

    public async Task HandleAsync(UpdatePlaybackStateCommand request)
    {
        await validator.ValidateAndThrowAsync(request);
        
        playbackTrackingService.UpdatePlaybackState(
            currentUserRepository.CurrentUserId,
            currentDeviceRepository.DeviceId,
            request.CurrentTime);
    }
}