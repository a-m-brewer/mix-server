using Microsoft.Extensions.Logging;
using MixServer.Domain.Exceptions;
using MixServer.Domain.FileExplorer.Services;
using MixServer.Domain.Persistence;
using MixServer.Domain.Sessions.Entities;
using MixServer.Domain.Sessions.Repositories;
using MixServer.Domain.Sessions.Requests;
using MixServer.Domain.Sessions.Services;
using MixServer.Domain.Users.Repositories;
using MixServer.Domain.Utilities;
using MixServer.Infrastructure.EF.Entities;
using MixServer.Infrastructure.Users.Repository;

namespace MixServer.Infrastructure.Sessions.Services;

public class SessionService(
    ICurrentDeviceRepository currentDeviceRepository,
    ICurrentUserRepository currentUserRepository,
    IDateTimeProvider dateTimeProvider,
    IFileService fileService,
    ILogger<SessionService> logger,
    IPlaybackSessionRepository playbackSessionRepository,
    IPlaybackTrackingService playbackTrackingService,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork)
    : ISessionService
{
    public async Task LoadPlaybackStateAsync()
    {
        if (playbackTrackingService.IsTracking(currentUserRepository.CurrentUserId))
        {
            logger.LogDebug("Skipping loading playback state as it is already being tracked");
            return;
        }

        var session = await GetCurrentPlaybackSessionOrDefaultAsync();

        if (session == null)
        {
            logger.LogDebug("Skipping loading playback state as there is currently no playback session to track");
            return;
        }

        playbackTrackingService.UpdateSessionState(session);
    }

    public async Task<PlaybackSession> AddOrUpdateSessionAsync(IAddOrUpdateSessionRequest request)
    {
        await currentUserRepository.LoadAllPlaybackSessionsAsync();
        var user = currentUserRepository.CurrentUser;
        
        var session = user.PlaybackSessions.SingleOrDefault(s => s.AbsolutePath == request.AbsoluteFilePath);

        if (session == null)
        {
            session = new PlaybackSession
            {
                Id = Guid.NewGuid(),
                AbsolutePath = request.AbsoluteFilePath,
                LastPlayed = dateTimeProvider.UtcNow,
                UserId = user.Id,
                CurrentTime = TimeSpan.Zero
            };

            await playbackSessionRepository.AddAsync(session);
            user.PlaybackSessions.Add(session);
        }
        else
        {
            session.LastPlayed = dateTimeProvider.UtcNow;
        }

        var hasState = playbackTrackingService.TryGet(user.Id, out var state);
        session.DeviceId = hasState
            ? state!.DeviceId
            : currentDeviceRepository.DeviceId;
        session.LastPlaybackDeviceId = hasState
            ? state!.LastPlaybackDeviceId
            : currentDeviceRepository.DeviceId;

        playbackTrackingService.UpdateSessionState(session);
        
        user.CurrentPlaybackSession = session;

        SetSessionNonMappedProperties(session);
        
        unitOfWork.InvokeCallbackOnSaved(c => c.CurrentSessionUpdated(session.UserId, session));
        return session;
    }

    public void ClearUsersCurrentSession()
    {
        var user = currentUserRepository.CurrentUser;
        
        if (user.CurrentPlaybackSession != null)
        {
            playbackTrackingService.ClearSession(user.Id);
        }
        
        user.CurrentPlaybackSession = null;
        
        unitOfWork.InvokeCallbackOnSaved(c => c.CurrentSessionUpdated(user.Id, null));
    }

    public async Task<PlaybackSession> GetPlaybackSessionByIdAsync(Guid id, string username)
    {
        var user = await userRepository.GetUserAsync(username);

        var session = await playbackSessionRepository.GetAsync(id);

        if (session.UserId != user.Id)
        {
            throw new ForbiddenRequestException();
        }

        SetSessionNonMappedProperties(session);
        
        return session;
    }

    public async Task<PlaybackSession> GetCurrentPlaybackSessionWithFileAsync()
    {
        var session = await GetCurrentPlaybackSessionAsync();
        
        SetSessionNonMappedProperties(session);

        return session;
    }

    public async Task<PlaybackSession> GetCurrentPlaybackSessionAsync()
    {
        var session = await GetCurrentPlaybackSessionOrDefaultAsync();

        if (session == null)
        {
            throw new NotFoundException(nameof(DbUser), nameof(DbUser.CurrentPlaybackSession));
        }

        return session;
    }

    public async Task<PlaybackSession?> GetCurrentPlaybackSessionOrDefaultAsync()
    {
        await currentUserRepository.LoadCurrentPlaybackSessionAsync();
        var user = currentUserRepository.CurrentUser;

        var session = user.CurrentPlaybackSession;

        return session;
    }

    public async Task<List<PlaybackSession>> GetUsersPlaybackSessionHistoryAsync(int startIndex, int pageSize)
    {
        await currentUserRepository.LoadPagedPlaybackSessionsAsync(startIndex, pageSize);
        var user = currentUserRepository.CurrentUser;

        var sessions = user.PlaybackSessions;

        foreach (var session in sessions)
        {
            SetSessionNonMappedProperties(session);
        }

        return sessions;
    }

    private void SetSessionNonMappedProperties(
        IPlaybackSession session)
    {
        var currentNode = fileService.GetFile(session.AbsolutePath);

        playbackTrackingService.Populate(session);

        session.File = currentNode;
    }
}