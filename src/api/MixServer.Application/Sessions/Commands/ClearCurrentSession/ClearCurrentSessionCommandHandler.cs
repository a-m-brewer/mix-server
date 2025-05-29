using MixServer.Application.Sessions.Converters;
using MixServer.Application.Sessions.Dtos;
using MixServer.Domain.Exceptions;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Persistence;
using MixServer.Domain.Queueing.Entities;
using MixServer.Domain.Queueing.Services;
using MixServer.Domain.Sessions.Services;
using MixServer.Infrastructure.Users.Repository;

namespace MixServer.Application.Sessions.Commands.ClearCurrentSession;

public class ClearCurrentSessionCommandHandler(
    IPlaybackSessionDtoConverter converter,
    ICurrentUserRepository currentUserRepository,
    IQueueService queueService,
    ISessionService sessionService,
    IUnitOfWork unitOfWork)
    : ICommandHandler<ClearCurrentSessionCommand, CurrentSessionUpdatedDto>
{
    public async Task<CurrentSessionUpdatedDto> HandleAsync(ClearCurrentSessionCommand request, CancellationToken cancellationToken = default)
    {
        await currentUserRepository.LoadCurrentPlaybackSessionAsync(cancellationToken);
        var user = await currentUserRepository.GetCurrentUserAsync();

        if (user.CurrentPlaybackSession == null)
        {
            throw new InvalidRequestException(nameof(user.CurrentPlaybackSession),"User currently had no playback session");
        }
        
        await sessionService.ClearUsersCurrentSessionAsync();
        queueService.ClearQueue();
        
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return converter.Convert(null, QueueSnapshot.Empty, false);
    }
}