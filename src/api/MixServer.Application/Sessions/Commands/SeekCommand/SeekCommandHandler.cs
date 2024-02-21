using FluentValidation;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Sessions.Services;
using MixServer.Infrastructure.Users.Repository;

namespace MixServer.Application.Sessions.Commands.SeekCommand;

public class SeekCommandHandler(
    ICurrentUserRepository currentUserRepository,
    IPlaybackTrackingService playbackTrackingService,
    IValidator<SeekCommand> validator)
    : ICommandHandler<SeekCommand>
{
    public async Task HandleAsync(SeekCommand request)
    {
        await validator.ValidateAndThrowAsync(request);
        
        playbackTrackingService.Seek(currentUserRepository.CurrentUserId, request.Time);
    }
}