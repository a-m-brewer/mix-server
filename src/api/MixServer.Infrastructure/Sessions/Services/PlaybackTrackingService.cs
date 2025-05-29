using System.Collections.Concurrent;
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
using MixServer.Infrastructure.Users.Repository;

namespace MixServer.Infrastructure.Sessions.Services;

public class PlaybackTrackingService(
    IUpdateSessionStateRateLimiter updateSessionStateRateLimiter,
    ILogger<PlaybackTrackingService> logger,
    ILoggerFactory loggerFactory,
    IServiceProvider serviceProvider)
    : IPlaybackTrackingService
{
    private readonly ConcurrentDictionary<string, PlaybackState> _playingItems = new();
    private readonly KeyedDebouncer<Guid> _saveSessionStateDebouncer = new(
        TimeSpan.FromSeconds(5),
        logger,
        TimeSpan.FromSeconds(30));

    public PlaybackState GetOrThrow(string userId)
    {
        if (_playingItems.TryGetValue(userId, out var state))
        {
            return state;
        }

        throw new NotFoundException(nameof(PlaybackState), userId);
    }

    public bool TryGet(string userId, [MaybeNullWhen(false)] out PlaybackState state)
    {
        return _playingItems.TryGetValue(userId, out state);
    }

    public bool IsTracking(string userId)
    {
        return _playingItems.ContainsKey(userId);
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
        var nodePath = session.NodeEntity.Path;
            
        if (_playingItems.TryGetValue(session.UserId, out var existingItem))
        {
            existingItem.UpdateWithoutEvents(session, nodePath, includePlaying);
        }
        else
        {
            logger.LogInformation("Started tracking user's ({UserId}) playback state", session.UserId);
                
            var newSession = new PlaybackState(session, loggerFactory.CreateLogger<PlaybackState>())
            {
                NodePath = nodePath
            };
            newSession.AudioPlayerStateUpdated += OnAudioPlayerStateUpdated;
                
            _playingItems[session.UserId] = newSession;
        }
    }

    public void ClearSession(string userId)
    {
        if (!_playingItems.TryGetValue(userId, out var state))
        {
            return;
        }

        state.ClearSession();
    }

    public void UpdateAudioPlayerCurrentTime(string userId, Guid requestingDeviceId, TimeSpan currentTime)
    {
        if (!_playingItems.TryGetValue(userId, out var state))
        {
            logger.LogWarning("Could not find playback state for user: {UserId}", userId);
            return;
        }

        if (!updateSessionStateRateLimiter.TryAcquire(userId))
        {
            logger.LogTrace("Failed to acquire lease to update user: {UserId}'s session due to to many requests", userId);
            return;
        }
            
        if (state.DeviceId != requestingDeviceId)
        {
            logger.LogWarning("Could not update playback current time for user: {UserId} due mismatching devices" + 
                              " Requesting Device: {RequestingDeviceId} State DeviceId: {StateDeviceId}",
                userId,
                requestingDeviceId,
                state.DeviceId);
            return;
        }

        if (!state.Playing)
        {
            logger.LogWarning("Could not update playback current time for user: {UserId} as the session is not playing", userId);
            return;
        }

        state.UpdateAudioPlayerCurrentTime(requestingDeviceId, currentTime);
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

        try
        {
            using var scope = serviceProvider.CreateScope();
            var callbackService = scope.ServiceProvider.GetRequiredService<ICallbackService>();

            await TrySavePlaybackStateAsync(playbackState, type);

            await callbackService.PlaybackStateUpdated(playbackState, type);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to update playback state");
        }
    }

    public void HandleDeviceDisconnected(string userId, Guid deviceId)
    {
        if (_playingItems.TryGetValue(userId, out var state))
        {
            state.HandleDeviceDisconnected(deviceId);
        }
    }

    public void Populate(IPlaybackSession session)
    {
        if (!_playingItems.TryGetValue(session.UserId, out var playingItem))
        {
            return;
        }

        session.PopulateState(playingItem);
    }

    private async Task TrySavePlaybackStateAsync(
        IPlaybackState playbackState,
        AudioPlayerStateUpdateType type,
        CancellationToken cancellationToken = default)
    {
        if (!playbackState.SessionId.HasValue || playbackState.SessionId.Value == Guid.Empty)
        {
            logger.LogWarning("Failed to save playback state as there is currently no session associated with User: {UserId}", playbackState.UserId);
            return;
        }
        
        if (type == AudioPlayerStateUpdateType.CurrentTime)
        {
            return;
        }

        await _saveSessionStateDebouncer.DebounceAsync(playbackState.SessionId.Value, async () =>
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var currentUserRepository = scope.ServiceProvider.GetRequiredService<ICurrentUserRepository>();
                var playbackSessionRepository = scope.ServiceProvider.GetRequiredService<IPlaybackSessionRepository>();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                logger.LogDebug("Saving Session: {SessionId}", playbackState.SessionId);
                currentUserRepository.SetUserId(playbackState.UserId);
                var currentUser = await currentUserRepository.GetCurrentUserAsync();

                var session = await playbackSessionRepository.GetAsync(playbackState.SessionId.Value, cancellationToken);

                if (currentUser.Id != session.UserId)
                {
                    return;
                }

                session.CurrentTime = playbackState.CurrentTime;

                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to save playback state");
            }
        }, cancellationToken);
    }
}