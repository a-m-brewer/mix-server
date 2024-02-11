using FluentValidation;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Sessions.Services;
using MixServer.Infrastructure.Users.Repository;

namespace MixServer.Application.Sessions.Commands.SeekCommand;

public class SeekCommandHandler : ICommandHandler<SeekCommand>
{
    private readonly ICurrentUserRepository _currentUserRepository;
    private readonly IPlaybackTrackingService _playbackTrackingService;
    private readonly IValidator<SeekCommand> _validator;

    public SeekCommandHandler(
        ICurrentUserRepository currentUserRepository,
        IPlaybackTrackingService playbackTrackingService,
        IValidator<SeekCommand> validator)
    {
        _currentUserRepository = currentUserRepository;
        _playbackTrackingService = playbackTrackingService;
        _validator = validator;
    }
    
    public async Task HandleAsync(SeekCommand request)
    {
        await _validator.ValidateAndThrowAsync(request);
        
        _playbackTrackingService.Seek(_currentUserRepository.CurrentUserId, request.Time);
    }
}