using FluentValidation;
using MixServer.Application.Sessions.Converters;
using MixServer.Application.Sessions.Dtos;
using MixServer.Domain.Exceptions;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Persistence;
using MixServer.Domain.Queueing.Entities;
using MixServer.Domain.Queueing.Enums;
using MixServer.Domain.Queueing.Services;
using MixServer.Domain.Sessions.Accessors;
using MixServer.Domain.Sessions.Entities;
using MixServer.Domain.Sessions.Requests;
using MixServer.Domain.Sessions.Services;
using MixServer.Domain.Sessions.Validators;

namespace MixServer.Application.Sessions.Commands.SetNextSession;

public class SetNextSessionCommandHandler(
    ICanPlayOnDeviceValidator canPlayOnDeviceValidator,
    IPlaybackSessionDtoConverter converter,
    IQueueService queueService,
    IPlaybackTrackingService playbackTrackingService,
    IRequestedPlaybackDeviceAccessor requestedPlaybackDeviceAccessor,
    ISessionService sessionService,
    IValidator<SetNextSessionCommand> validator,
    IUnitOfWork unitOfWork)
    : ICommandHandler<SetNextSessionCommand, CurrentSessionUpdatedDto>
{
    public async Task<CurrentSessionUpdatedDto> HandleAsync(SetNextSessionCommand request)
    {
        await validator.ValidateAndThrowAsync(request);
        
        var currentSession = await sessionService.GetCurrentPlaybackSessionAsync();

        if (request.ResetSessionState)
        {
            currentSession.CurrentTime = TimeSpan.Zero;
            playbackTrackingService.ClearSession(currentSession.UserId);
        }

        var (result, queueItem) = await queueService.GetQueueSnapshotItemAsync(request.Offset);

        IPlaybackSession? nextSession = null;
        QueueSnapshot queueSnapshot;
        switch (result)
        {
            case PlaylistIncrementResult.PreviousOutOfBounds:
                throw new InvalidRequestException(nameof(request.Offset),"Next file can not be before the start of the playlist");
            case PlaylistIncrementResult.Success:
                canPlayOnDeviceValidator.ValidateCanPlayOrThrow(requestedPlaybackDeviceAccessor.PlaybackDevice, queueItem!.File);

                queueSnapshot = await queueService.SetQueuePositionAsync(queueItem);
                
                var nextFile = queueItem.File;
                
                nextSession = await sessionService.AddOrUpdateSessionAsync(new AddOrUpdateSessionRequest
                {
                    ParentAbsoluteFilePath = nextFile.Parent.AbsolutePath,
                    FileName = nextFile.Name
                });
                break;
            case PlaylistIncrementResult.NextOutOfBounds:
                sessionService.ClearUsersCurrentSession();
                queueSnapshot = QueueSnapshot.Empty;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        
        await unitOfWork.SaveChangesAsync();

        return converter.Convert(nextSession, queueSnapshot, true);
    }
}