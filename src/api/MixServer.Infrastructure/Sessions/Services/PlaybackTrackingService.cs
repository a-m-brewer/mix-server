using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MixServer.Domain.Callbacks;
using MixServer.Domain.Exceptions;
using MixServer.Domain.Persistence;
using MixServer.Domain.Sessions.Entities;
using MixServer.Domain.Sessions.Enums;
using MixServer.Domain.Sessions.Models;
using MixServer.Domain.Sessions.Repositories;
using MixServer.Domain.Sessions.Services;
using MixServer.Domain.Utilities;
using MixServer.Infrastructure.Users.Repository;

namespace MixServer.Infrastructure.Sessions.Services;

public class PlaybackTrackingService : IPlaybackTrackingService
{
    private readonly Dictionary<string, PlaybackState> _playingItems = new();

    private readonly ISaveSessionStateRateLimiter _saveSessionStateRateLimiter;
    private readonly IUpdateSessionStateRateLimiter _updateSessionStateRateLimiter;
    private readonly ILogger<PlaybackTrackingService> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IReadWriteLock _readWriteLock;
    private readonly IServiceProvider _serviceProvider;

    public PlaybackTrackingService(
        ISaveSessionStateRateLimiter saveSessionStateRateLimiter,
        IUpdateSessionStateRateLimiter updateSessionStateRateLimiter,
        ILogger<PlaybackTrackingService> logger,
        ILoggerFactory loggerFactory,
        IReadWriteLock readWriteLock,
        IServiceProvider serviceProvider)
    {
        _saveSessionStateRateLimiter = saveSessionStateRateLimiter;
        _updateSessionStateRateLimiter = updateSessionStateRateLimiter;
        _logger = logger;
        _loggerFactory = loggerFactory;
        _readWriteLock = readWriteLock;
        _serviceProvider = serviceProvider;
    }

    public PlaybackState GetOrThrow(string userId)
    {
        return _readWriteLock.ForRead(() =>
        {
            if (_playingItems.TryGetValue(userId, out var state))
            {
                return state;
            }

            throw new NotFoundException(nameof(PlaybackState), userId);
        });
    }

    public bool TryGet(string userId, [MaybeNullWhen(false)] out PlaybackState state)
    {
        var res = _readWriteLock.ForRead(() =>
        {
            var found = _playingItems.TryGetValue(userId, out var existingState);

            return (found, existingState);
        });

        state = res.existingState;
        return res.found;
    }

    public bool IsTracking(string userId)
    {
        return _readWriteLock.ForRead(() => _playingItems.ContainsKey(userId));
    }

    public void UpdateSessionState(IPlaybackSession session)
    {
        UpdateSessionState(session, false);
    }

    public void UpdateSessionStateIncludingPlaying(IPlaybackSession session)
    {
        UpdateSessionState(session, true);
    }

    private void UpdateSessionState(IPlaybackSession session, bool includePlaying)
    {
        _readWriteLock.ForWrite(() =>
        {
            if (_playingItems.TryGetValue(session.UserId, out var existingItem))
            {
                existingItem.UpdateWithoutEvents(session, includePlaying);
            }
            else
            {
                _logger.LogInformation("Started tracking user's ({UserId}) playback state", session.UserId);
                
                var newSession = new PlaybackState(session, _loggerFactory.CreateLogger<PlaybackState>());
                newSession.AudioPlayerStateUpdated += OnAudioPlayerStateUpdated;
                
                _playingItems[session.UserId] = newSession;
            }
        });
    }

    public void ClearSession(string userId)
    {
        _readWriteLock.ForWrite(() =>
        {
            if (!_playingItems.TryGetValue(userId, out var state))
            {
                return;
            }

            state.ClearSession();
        });
    }

    public void UpdatePlaybackState(string userId, Guid requestingDeviceId, TimeSpan currentTime)
    {
        _readWriteLock.ForWrite(() =>
        {
            if (!_playingItems.TryGetValue(userId, out var state))
            {
                _logger.LogWarning("Could not find playback state for user: {UserId}", userId);
                return;
            }

            if (!_updateSessionStateRateLimiter.TryAcquire(userId))
            {
                _logger.LogTrace("Failed to acquire lease to update user: {UserId}'s session due to to many requests", userId);
                return;
            }
            
            if (state.DeviceId != requestingDeviceId)
            {
                _logger.LogWarning("Could not update playback state for user: {UserId} due mismatching devices" + 
                                   " Requesting Device: {RequestingDeviceId} State DeviceId: {StateDeviceId}",
                    userId,
                    requestingDeviceId,
                    state.DeviceId);
                return;
            }

            state.UpdateAudioPlayerState(requestingDeviceId, currentTime);
        });
    }

    public void UpdatePlaybackDevice(string userId, Guid deviceId)
    {
        _readWriteLock.ForWrite(() =>
        {
            if (!_playingItems.TryGetValue(userId, out var state))
            {
                return;
            }

            state.DeviceId = deviceId;
        });
    }

    public void SetWaitingForPause(string userId)
    {
        if (!_playingItems.TryGetValue(userId, out var state))
        {
            throw new NotFoundException(nameof(PlaybackState), userId);
        }

        state.SetWaitingForPause();
    }

    public void WaitForPause(string userId)
    {
        if (!_playingItems.TryGetValue(userId, out var state))
        {
            throw new NotFoundException(nameof(PlaybackState), userId);
        }

        state.WaitForPause();
    }

    public void SetPlaying(string userId, bool playing, TimeSpan currentTime)
    {
        if (!_playingItems.TryGetValue(userId, out var state))
        {
            throw new NotFoundException(nameof(PlaybackState), userId);
        }
            
        state.SetPlaying(playing, currentTime);
    }

    public void Seek(string userId, TimeSpan time)
    {
        if (!_playingItems.TryGetValue(userId, out var state))
        {
            throw new NotFoundException(nameof(PlaybackState), userId);
        }

        state.Seek(time);
    }
    
    private async void OnAudioPlayerStateUpdated(object? sender, AudioPlayerStateUpdateType type)
    {
        if (sender is not IPlaybackState playbackState)
        {
            return;
        }
        
        using var scope = _serviceProvider.CreateScope();
        var callbackService = scope.ServiceProvider.GetRequiredService<ICallbackService>();

        await callbackService.PlaybackStateUpdated(playbackState, type);
        
        await TrySavePlaybackStateAsync(playbackState, type);
    }

    public void HandleDeviceDisconnected(string userId, Guid deviceId)
    {
        _readWriteLock.ForUpgradeableRead(() =>
        {
            if (_playingItems.TryGetValue(userId, out var state))
            {
                _readWriteLock.ForWrite(() =>
                {
                    state.HandleDeviceDisconnected(deviceId);
                });
            }
        });
    }

    public void Populate(IPlaybackSession session)
    {
        _readWriteLock.ForRead(() =>
        {
            if (!_playingItems.TryGetValue(session.UserId, out var playingItem))
            {
                return;
            }

            session.PopulateState(playingItem);
        });
    }

    private async Task TrySavePlaybackStateAsync(IPlaybackState playbackState, AudioPlayerStateUpdateType type)
    {
        if (!playbackState.SessionId.HasValue || playbackState.SessionId.Value == Guid.Empty)
        {
            _logger.LogWarning("Failed to save playback state as there is currently no session associated with User: {UserId}", playbackState.UserId);
            return;
        }
        
        if (type == AudioPlayerStateUpdateType.CurrentTime && !_saveSessionStateRateLimiter.TryAcquire(playbackState.SessionId.Value))
        {
            _logger.LogTrace("Failed to acquire lease to save session: {SessionId} due to to many requests", playbackState.SessionId);
            return;
        }
        
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var currentUserRepository = scope.ServiceProvider.GetRequiredService<ICurrentUserRepository>();
            var playbackSessionRepository = scope.ServiceProvider.GetRequiredService<IPlaybackSessionRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            _logger.LogDebug("Saving Session: {SessionId}", playbackState.SessionId);
            await currentUserRepository.LoadUserAsync(playbackState.UserId);
            var currentUser = currentUserRepository.CurrentUser;

            var session = await playbackSessionRepository.GetAsync(playbackState.SessionId.Value);

            if (currentUser.Id != session.UserId)
            {
                return;
            }

            session.CurrentTime = playbackState.CurrentTime;

            await unitOfWork.SaveChangesAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to save playback state");
        }
    }
}