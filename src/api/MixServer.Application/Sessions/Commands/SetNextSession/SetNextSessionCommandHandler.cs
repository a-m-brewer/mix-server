using MixServer.Application.Sessions.Converters;
using MixServer.Application.Sessions.Dtos;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Persistence;
using MixServer.Domain.Queueing.Models;
using MixServer.Domain.Queueing.Repositories;
using MixServer.Domain.Queueing.Services;
using MixServer.Domain.Sessions.Requests;
using MixServer.Domain.Sessions.Services;
using MixServer.Infrastructure.Users.Repository;

namespace MixServer.Application.Sessions.Commands.SetNextSession;

public interface ISetNextSessionCommandHandler : ICommandHandler
{
    Task<CurrentSessionUpdatedDto> BackAsync(CancellationToken cancellationToken);
    Task<CurrentSessionUpdatedDto> SkipAsync(CancellationToken cancellationToken);
    Task<CurrentSessionUpdatedDto> EndAsync(CancellationToken cancellationToken);
}

public class SetNextSessionCommandHandler(
    IPlaybackSessionDtoConverter converter,
    IUserQueueService userQueueService,
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
        var currentSession = await sessionService.GetCurrentPlaybackSessionAsync(cancellationToken);

        if (resetSessionState)
        {
            currentSession.CurrentTime = TimeSpan.Zero;
            playbackTrackingService.ClearSession(currentSession.UserId);
        }

        var position = await userQueueService.GetQueuePositionAsync(cancellationToken: cancellationToken);
        
        var nextQueuePosition = skip
            ? position.Next
            : position.Previous;
        if (nextQueuePosition is null)
        {
            await sessionService.ClearUsersCurrentSessionAsync();
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return converter.Convert(null, QueuePosition.Empty, true);
        }
        
        await userQueueService.SetQueuePositionAsync(nextQueuePosition.Id, cancellationToken);
        
        position = await userQueueService.GetQueuePositionAsync(cancellationToken: cancellationToken);

        var nextFile = position.Current?.File;
        var nextSession = nextFile is null
            ? null
            : await sessionService.AddOrUpdateSessionAsync(new AddOrUpdateSessionRequest
            {
                NodePath = nextFile.Path
            }, cancellationToken);
        
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return converter.Convert(nextSession, position, true);
    }
}