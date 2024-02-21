using MixServer.Domain.Callbacks;
using MixServer.Domain.Exceptions;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Persistence;
using MixServer.Domain.Queueing.Services;
using MixServer.Domain.Sessions.Services;
using MixServer.Infrastructure.Users.Repository;

namespace MixServer.Application.Sessions.Commands.ClearCurrentSession;

public class ClearCurrentSessionCommandHandler(
    ICallbackService callbackService,
    ICurrentUserRepository currentUserRepository,
    IQueueService queueService,
    ISessionService sessionService,
    IUnitOfWork unitOfWork)
    : ICommandHandler<ClearCurrentSessionCommand>
{
    private readonly ICallbackService _callbackService = callbackService;

    public async Task HandleAsync(ClearCurrentSessionCommand request)
    {
        await currentUserRepository.LoadCurrentPlaybackSessionAsync();
        var user = currentUserRepository.CurrentUser;

        if (user.CurrentPlaybackSession == null)
        {
            throw new InvalidRequestException(nameof(user.CurrentPlaybackSession),"User currently had no playback session");
        }
        
        sessionService.ClearUsersCurrentSession();
        queueService.ClearQueue();
        
        await unitOfWork.SaveChangesAsync();
    }
}