using FluentValidation;
using Microsoft.Extensions.Logging;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Sessions.Services;
using MixServer.Domain.Users.Repositories;
using MixServer.Infrastructure.Users.Repository;

namespace MixServer.Application.Sessions.Commands.UpdatePlaybackState;

public class UpdatePlaybackStateCommandHandler : ICommandHandler<UpdatePlaybackStateCommand>
{
    private readonly ICurrentDeviceRepository _currentDeviceRepository;
    private readonly ICurrentUserRepository _currentUserRepository;
    private readonly ILogger<UpdatePlaybackStateCommandHandler> _logger;
    private readonly IPlaybackTrackingService _playbackTrackingService;
    private readonly IValidator<UpdatePlaybackStateCommand> _validator;

    public UpdatePlaybackStateCommandHandler(
        ICurrentDeviceRepository currentDeviceRepository,
        ICurrentUserRepository currentUserRepository,
        ILogger<UpdatePlaybackStateCommandHandler> logger,
        IPlaybackTrackingService playbackTrackingService,
        IValidator<UpdatePlaybackStateCommand> validator)
    {
        _currentDeviceRepository = currentDeviceRepository;
        _currentUserRepository = currentUserRepository;
        _logger = logger;
        _playbackTrackingService = playbackTrackingService;
        _validator = validator;
    }

    public async Task HandleAsync(UpdatePlaybackStateCommand request)
    {
        await _validator.ValidateAndThrowAsync(request);
        
        _playbackTrackingService.UpdatePlaybackState(
            _currentUserRepository.CurrentUserId,
            _currentDeviceRepository.DeviceId,
            request.CurrentTime);
    }
}