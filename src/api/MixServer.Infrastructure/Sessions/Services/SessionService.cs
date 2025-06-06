﻿using MixServer.Domain.Exceptions;
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
    IPlaybackSessionRepository playbackSessionRepository,
    IPlaybackTrackingService playbackTrackingService,
    IRequestedPlaybackDeviceAccessor requestedPlaybackDeviceAccessor,
    ISessionHydrationService sessionHydrationService,
    IUnitOfWork unitOfWork)
    : ISessionService
{
    public async Task<PlaybackSession> AddOrUpdateSessionAsync(
        IAddOrUpdateSessionRequest request,
        CancellationToken cancellationToken)
    {
        var user = await currentUserRepository.GetCurrentUserAsync();

        var device = await requestedPlaybackDeviceAccessor.GetPlaybackDeviceAsync();
        
        var file = await folderPersistenceService.GetFileAsync(request.NodePath, cancellationToken);

        canPlayOnDeviceValidator.ValidateCanPlayOrThrow(device, file);

        await currentUserRepository.LoadPlaybackSessionByFileIdAsync(file.Entity.Id, cancellationToken);
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

            await playbackSessionRepository.AddAsync(session, cancellationToken);
            user.PlaybackSessions.Add(session);
        }
        else
        {
            session.LastPlayed = dateTimeProvider.UtcNow;
        }

        session.DeviceId = device.DeviceId;

        playbackTrackingService.UpdateSessionState(session);
        
        user.CurrentPlaybackSession = session;

        await sessionHydrationService.HydrateAsync(session);
        
        unitOfWork.InvokeCallbackOnSaved(c => c.CurrentSessionUpdated(session.UserId, currentDeviceRepository.DeviceId, session));
        return session;
    }

    public async Task ClearUsersCurrentSessionAsync()
    {
        var user = await currentUserRepository.GetCurrentUserAsync();
        
        if (user.CurrentPlaybackSession != null)
        {
            playbackTrackingService.ClearSession(user.Id);
        }
        
        user.CurrentPlaybackSession = null;
        
        unitOfWork.InvokeCallbackOnSaved(c => c.CurrentSessionUpdated(user.Id, currentDeviceRepository.DeviceId, null));
    }

    public async Task<PlaybackSession> GetPlaybackSessionByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var session = await playbackSessionRepository.GetAsync(id, cancellationToken);

        await sessionHydrationService.HydrateAsync(session);
        
        return session;
    }

    public async Task<PlaybackSession> GetCurrentPlaybackSessionWithFileAsync(CancellationToken cancellationToken)
    {
        var session = await GetCurrentPlaybackSessionAsync(cancellationToken);
        
        await sessionHydrationService.HydrateAsync(session);

        return session;
    }

    public async Task<PlaybackSession> GetCurrentPlaybackSessionAsync(CancellationToken cancellationToken)
    {
        var session = await GetCurrentPlaybackSessionOrDefaultAsync(cancellationToken);

        if (session == null)
        {
            throw new NotFoundException(nameof(DbUser), nameof(DbUser.CurrentPlaybackSession));
        }

        return session;
    }

    public async Task<PlaybackSession?> GetCurrentPlaybackSessionOrDefaultAsync(CancellationToken cancellationToken)
    {
        await currentUserRepository.LoadCurrentPlaybackSessionAsync(cancellationToken);
        var user = await currentUserRepository.GetCurrentUserAsync();

        var session = user.CurrentPlaybackSession;

        return session;
    }

    public async Task<List<PlaybackSession>> GetUsersPlaybackSessionHistoryAsync(int startIndex, int pageSize,
        CancellationToken cancellationToken)
    {
        await currentUserRepository.LoadPagedPlaybackSessionsAsync(startIndex, pageSize, cancellationToken);
        var user = await currentUserRepository.GetCurrentUserAsync();

        var sessions = user.PlaybackSessions;

        await Task.WhenAll(sessions.Select(sessionHydrationService.HydrateAsync));

        return sessions;
    }
}