using MixServer.Domain.Callbacks;
using MixServer.Domain.Exceptions;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Persistence;
using MixServer.Domain.Queueing.Services;
using MixServer.Domain.Sessions.Services;
using MixServer.Infrastructure.Users.Repository;

namespace MixServer.Application.Sessions.Commands.ClearCurrentSession;

public class ClearCurrentSessionCommandHandler : ICommandHandler<ClearCurrentSessionCommand>
{
    private readonly ICallbackService _callbackService;
    private readonly ICurrentUserRepository _currentUserRepository;
    private readonly IQueueService _queueService;
    private readonly ISessionService _sessionService;
    private readonly IUnitOfWork _unitOfWork;

    public ClearCurrentSessionCommandHandler(
        ICallbackService callbackService,
        ICurrentUserRepository currentUserRepository,
        IQueueService queueService,
        ISessionService sessionService,
        IUnitOfWork unitOfWork)
    {
        _callbackService = callbackService;
        _currentUserRepository = currentUserRepository;
        _queueService = queueService;
        _sessionService = sessionService;
        _unitOfWork = unitOfWork;
    }
    
    public async Task HandleAsync(ClearCurrentSessionCommand request)
    {
        await _currentUserRepository.LoadCurrentPlaybackSessionAsync();
        var user = _currentUserRepository.CurrentUser;

        if (user.CurrentPlaybackSession == null)
        {
            throw new InvalidRequestException(nameof(user.CurrentPlaybackSession),"User currently had no playback session");
        }
        
        _sessionService.ClearUsersCurrentSession();
        _queueService.ClearQueue();
        
        await _unitOfWork.SaveChangesAsync();
    }
}