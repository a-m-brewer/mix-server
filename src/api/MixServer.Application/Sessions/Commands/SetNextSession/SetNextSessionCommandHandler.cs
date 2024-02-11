using FluentValidation;
using MixServer.Domain.Exceptions;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Persistence;
using MixServer.Domain.Queueing.Enums;
using MixServer.Domain.Queueing.Services;
using MixServer.Domain.Sessions.Requests;
using MixServer.Domain.Sessions.Services;

namespace MixServer.Application.Sessions.Commands.SetNextSession;

public class SetNextSessionCommandHandler : ICommandHandler<SetNextSessionCommand>
{
    private readonly IQueueService _queueService;
    private readonly IPlaybackTrackingService _playbackTrackingService;
    private readonly ISessionService _sessionService;
    private readonly IValidator<SetNextSessionCommand> _validator;
    private readonly IUnitOfWork _unitOfWork;

    public SetNextSessionCommandHandler(
        IQueueService queueService,
        IPlaybackTrackingService playbackTrackingService,
        ISessionService sessionService,
        IValidator<SetNextSessionCommand> validator,
        IUnitOfWork unitOfWork)
    {
        _queueService = queueService;
        _playbackTrackingService = playbackTrackingService;
        _sessionService = sessionService;
        _validator = validator;
        _unitOfWork = unitOfWork;
    }
    
    public async Task HandleAsync(SetNextSessionCommand request)
    {
        await _validator.ValidateAndThrowAsync(request);
        
        var currentSession = await _sessionService.GetCurrentPlaybackSessionAsync();

        if (request.ResetSessionState)
        {
            currentSession.CurrentTime = TimeSpan.Zero;
            _playbackTrackingService.ClearSession(currentSession.UserId);
        }

        var (result, snapshot) = await _queueService.IncrementQueuePositionAsync(request.Offset);

        switch (result)
        {
            case PlaylistIncrementResult.PreviousOutOfBounds:
                throw new InvalidRequestException(nameof(request.Offset),"Next file can not be before the start of the playlist");
            case PlaylistIncrementResult.Success:
                var nextFile = snapshot.CurrentQueuePositionItem?.File;
                if (nextFile != null)
                {
                    await _sessionService.AddOrUpdateSessionAsync(new AddOrUpdateSessionRequest
                    {
                        ParentAbsoluteFilePath = nextFile.Parent.AbsolutePath,
                        FileName = nextFile.Name
                    });
                }
                break;
            case PlaylistIncrementResult.NextOutOfBounds:
                _sessionService.ClearUsersCurrentSession();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        
        await _unitOfWork.SaveChangesAsync();
    }
}