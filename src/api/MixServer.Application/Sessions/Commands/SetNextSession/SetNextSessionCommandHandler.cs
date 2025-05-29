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
    Task<CurrentSessionUpdatedDto> BackAsync(CancellationToken cancellationToken);
    Task<CurrentSessionUpdatedDto> SkipAsync(CancellationToken cancellationToken);
    Task<CurrentSessionUpdatedDto> EndAsync(CancellationToken cancellationToken);
}

public class SetNextSessionCommandHandler(
    IPlaybackSessionDtoConverter converter,
    IQueueService queueService,
    IPlaybackTrackingService playbackTrackingService,
    ISessionService sessionService,
    IUnitOfWork unitOfWork)
    : ISetNextSessionCommandHandler
{
    public Task<CurrentSessionUpdatedDto> BackAsync(CancellationToken cancellationToken) =>
        SetNextPositionAsync(false, false, cancellationToken);
    public Task<CurrentSessionUpdatedDto> SkipAsync(CancellationToken cancellationToken) =>
        SetNextPositionAsync(true, false, cancellationToken);
    public Task<CurrentSessionUpdatedDto> EndAsync(CancellationToken cancellationToken) =>
        SetNextPositionAsync(true, true, cancellationToken);
    
    private async Task<CurrentSessionUpdatedDto> SetNextPositionAsync(bool skip, bool resetSessionState, CancellationToken cancellationToken)
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
            await sessionService.ClearUsersCurrentSessionAsync();
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return converter.Convert(null, QueueSnapshot.Empty, true);
        }
        
        queue = await queueService.SetQueuePositionAsync(nextQueuePosition.Value);

        var nextFile = queue.CurrentQueuePositionItem?.File;
        var nextSession = nextFile is null
            ? null
            : await sessionService.AddOrUpdateSessionAsync(new AddOrUpdateSessionRequest
            {
                NodePath = nextFile.Path
            }, cancellationToken);
        
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return converter.Convert(nextSession, queue, true);
    }
}