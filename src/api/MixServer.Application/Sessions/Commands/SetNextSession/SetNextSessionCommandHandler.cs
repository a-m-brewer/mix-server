using MixServer.Application.Sessions.Converters;
using MixServer.Application.Sessions.Dtos;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Persistence;
using MixServer.Domain.Queueing.Entities;
using MixServer.Domain.Queueing.Services;
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
    IPlaybackSessionDtoConverter converter,
    IQueueService queueService,
    IPlaybackTrackingService playbackTrackingService,
    ISessionService sessionService,
    IUnitOfWork unitOfWork)
    : ISetNextSessionCommandHandler
{
    public Task<CurrentSessionUpdatedDto> BackAsync() => SetNextPositionAsync(false, false);
    public Task<CurrentSessionUpdatedDto> SkipAsync() => SetNextPositionAsync(true, false);
    public Task<CurrentSessionUpdatedDto> EndAsync() => SetNextPositionAsync(true, true);
    
    private async Task<CurrentSessionUpdatedDto> SetNextPositionAsync(bool skip, bool resetSessionState)
    {
        var currentSession = await sessionService.GetCurrentPlaybackSessionAsync();

        if (resetSessionState)
        {
            currentSession.CurrentTime = TimeSpan.Zero;
            playbackTrackingService.ClearSession(currentSession.UserId);
        }

        var queue = await queueService.GenerateQueueSnapshotAsync();
        
        var nextQueuePosition = skip
            ? queue.NextQueuePosition
            : queue.PreviousQueuePosition;
        if (!nextQueuePosition.HasValue)
        {
            sessionService.ClearUsersCurrentSession();
            await unitOfWork.SaveChangesAsync();
            return converter.Convert(null, QueueSnapshot.Empty, true);
        }
        
        queue = await queueService.SetQueuePositionAsync(nextQueuePosition.Value);

        var nextFile = queue.CurrentQueuePositionItem?.File;
        var nextSession = nextFile is null
            ? null
            : await sessionService.AddOrUpdateSessionAsync(new AddOrUpdateSessionRequest
            {
                NodePath = nextFile.Path
            });
        
        await unitOfWork.SaveChangesAsync();

        return converter.Convert(nextSession, queue, true);
    }
}