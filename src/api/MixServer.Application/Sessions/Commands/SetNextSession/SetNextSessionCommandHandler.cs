using MixServer.Application.Sessions.Converters;
using MixServer.Application.Sessions.Dtos;
using MixServer.Domain.Exceptions;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Persistence;
using MixServer.Domain.Queueing.Entities;
using MixServer.Domain.Queueing.Enums;
using MixServer.Domain.Queueing.Services;
using MixServer.Domain.Sessions.Entities;
using MixServer.Domain.Sessions.Requests;
using MixServer.Domain.Sessions.Services;
using MixServer.Domain.Sessions.Validators;

namespace MixServer.Application.Sessions.Commands.SetNextSession;

public interface ISetNextSessionCommandHandler : ICommandHandler
{
    Task<CurrentSessionUpdatedDto> BackAsync();
    Task<CurrentSessionUpdatedDto> SkipAsync();
    Task<CurrentSessionUpdatedDto> EndAsync();
}

public class SetNextSessionCommandHandler(
    ICanPlayOnDeviceValidator canPlayOnDeviceValidator,
    IPlaybackSessionDtoConverter converter,
    IQueueService queueService,
    IPlaybackTrackingService playbackTrackingService,
    ISessionService sessionService,
    IUnitOfWork unitOfWork)
    : ISetNextSessionCommandHandler
{
    public Task<CurrentSessionUpdatedDto> BackAsync() => SetNextPositionAsync(-1, false);
    public Task<CurrentSessionUpdatedDto> SkipAsync() => SetNextPositionAsync(1, false);
    public Task<CurrentSessionUpdatedDto> EndAsync() => SetNextPositionAsync(1, true);
    
    private async Task<CurrentSessionUpdatedDto> SetNextPositionAsync(int requestedOffset, bool resetSessionState)
    {
        var queue = await queueService.GenerateQueueSnapshotAsync();
        
        var offset = GetNextValidOffset(requestedOffset, queue);
        if (!offset.HasValue)
        {
            sessionService.ClearUsersCurrentSession();
            await unitOfWork.SaveChangesAsync();
            return converter.Convert(null, QueueSnapshot.Empty, true);
        }
        
        var currentSession = await sessionService.GetCurrentPlaybackSessionAsync();

        if (resetSessionState)
        {
            currentSession.CurrentTime = TimeSpan.Zero;
            playbackTrackingService.ClearSession(currentSession.UserId);
        }

        var (result, queueSnapshot) = await queueService.IncrementQueuePositionAsync(offset.Value);

        IPlaybackSession? nextSession = null;
        switch (result)
        {
            case PlaylistIncrementResult.PreviousOutOfBounds:
                throw new InvalidRequestException(nameof(offset),"Next file can not be before the start of the playlist");
            case PlaylistIncrementResult.Success 
                when queueSnapshot.CurrentQueuePositionItem?.File is not null:
                var nextFile = queueSnapshot.CurrentQueuePositionItem.File;
                nextSession = await sessionService.AddOrUpdateSessionAsync(new AddOrUpdateSessionRequest
                {
                    ParentAbsoluteFilePath = nextFile.Parent.AbsolutePath,
                    FileName = nextFile.Name
                });
                break;
            case PlaylistIncrementResult.Success:
                throw new NotFoundException(nameof(QueueSnapshot), "Current queue position");
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

    private int? GetNextValidOffset(int requestedOffset, QueueSnapshot queue)
    {
        var currentIndex = queue.Items.FindIndex(f => f.Id == queue.CurrentQueuePosition);
        var offsetIndex = currentIndex + requestedOffset;

        while (offsetIndex >= 0 && offsetIndex < queue.Items.Count)
        {
            var item = queue.Items[offsetIndex];
            if (canPlayOnDeviceValidator.CanPlay(item.File))
            {
                return offsetIndex - currentIndex;
            }

            var increment = requestedOffset < 0 ? -1 : 1;
            offsetIndex += increment;
        }

        return null;
    }
}