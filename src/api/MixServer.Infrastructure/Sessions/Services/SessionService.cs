using Microsoft.Extensions.Logging;
using MixServer.Domain.Exceptions;
using MixServer.Domain.FileExplorer.Services.Caching;
using MixServer.Domain.Persistence;
using MixServer.Domain.Sessions.Accessors;
using MixServer.Domain.Sessions.Entities;
using MixServer.Domain.Sessions.Repositories;
using MixServer.Domain.Sessions.Requests;
using MixServer.Domain.Sessions.Services;
using MixServer.Domain.Sessions.Validators;
using MixServer.Domain.Users.Repositories;
using MixServer.Domain.Utilities;
using MixServer.Infrastructure.EF.Entities;
using MixServer.Infrastructure.Users.Repository;

namespace MixServer.Infrastructure.Sessions.Services;

public class SessionService(
    ICanPlayOnDeviceValidator canPlayOnDeviceValidator,
    ICurrentDeviceRepository currentDeviceRepository,
    ICurrentUserRepository currentUserRepository,
    IDateTimeProvider dateTimeProvider,
    IFolderPersistenceService folderPersistenceService,
    ILogger<SessionService> logger,
    IPlaybackSessionRepository playbackSessionRepository,
    IPlaybackTrackingService playbackTrackingService,
    IRequestedPlaybackDeviceAccessor requestedPlaybackDeviceAccessor,
    ISessionHydrationService sessionHydrationService,
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
        var user = currentUserRepository.CurrentUser;
        
        var device = requestedPlaybackDeviceAccessor.PlaybackDevice;
        
        var file = await folderPersistenceService.GetFileAsync(request.NodePath);

        canPlayOnDeviceValidator.ValidateCanPlayOrThrow(device, file);

        await currentUserRepository.LoadPlaybackSessionByFileIdAsync(file.Entity.Id);
        var session = user.PlaybackSessions.SingleOrDefault(s => s.NodeEntity.Id == file.Entity.Id);

        if (session == null)
        {
            session = new PlaybackSession
            {
                Id = Guid.NewGuid(),
                LastPlayed = dateTimeProvider.UtcNow,
                UserId = user.Id,
                CurrentTime = TimeSpan.Zero,
                NodeEntity = file.Entity,
                NodeIdEntity = file.Entity.Id,
            };

            await playbackSessionRepository.AddAsync(session);
            user.PlaybackSessions.Add(session);
        }
        else
        {
            session.LastPlayed = dateTimeProvider.UtcNow;
        }

        session.DeviceId = device.DeviceId;

        playbackTrackingService.UpdateSessionState(session);
        
        user.CurrentPlaybackSession = session;

        sessionHydrationService.Hydrate(session);
        
        unitOfWork.InvokeCallbackOnSaved(c => c.CurrentSessionUpdated(session.UserId, currentDeviceRepository.DeviceId, session));
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
        
        unitOfWork.InvokeCallbackOnSaved(c => c.CurrentSessionUpdated(user.Id, currentDeviceRepository.DeviceId, null));
    }

    public async Task<PlaybackSession> GetPlaybackSessionByIdAsync(Guid id)
    {
        var session = await playbackSessionRepository.GetAsync(id);

        sessionHydrationService.Hydrate(session);
        
        return session;
    }

    public async Task<PlaybackSession> GetCurrentPlaybackSessionWithFileAsync()
    {
        var session = await GetCurrentPlaybackSessionAsync();
        
        sessionHydrationService.Hydrate(session);

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
            sessionHydrationService.Hydrate(session);
        }

        return sessions;
    }
}