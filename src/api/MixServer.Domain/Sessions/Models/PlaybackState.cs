using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using MixServer.Domain.Exceptions;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.Sessions.Entities;
using MixServer.Domain.Sessions.Enums;

namespace MixServer.Domain.Sessions.Models;

public interface IPlaybackState
{
    string UserId { get; }
    Guid? SessionId { get; }
    Guid? LastPlaybackDeviceId { get; }
    Guid? DeviceId { get; }
    bool Playing { get; }
    TimeSpan CurrentTime { get; }
}

public class PlaybackState(IPlaybackState session, ILogger<PlaybackState> logger) : IPlaybackState
{
    private readonly ManualResetEventSlim _pauseSemaphore = new(true);
    private readonly object _lock = new();

    private Guid? _deviceId = session.DeviceId;
    private Guid? _lastPlaybackDeviceId = session.LastPlaybackDeviceId;
    private bool _playing = session.Playing;
    private TimeSpan _currentTime = session.CurrentTime;
    private Guid? _sessionId = session.SessionId;
    private NodePath? _nodePath;

    public event EventHandler<AudioPlayerStateUpdateType>? AudioPlayerStateUpdated;

    public string UserId { get; } = session.UserId;

    public Guid? SessionId
    {
        get { lock (_lock) return _sessionId; }
    }

    public required NodePath? NodePath
    {
        get { lock (_lock) return _nodePath; }
        init { lock (_lock) _nodePath = value; }
    }

    public Guid? LastPlaybackDeviceId
    {
        get { lock (_lock) return _lastPlaybackDeviceId; }
    }
    
    public Guid? DeviceId
    {
        get
        {
            lock (_lock) return _deviceId;
        }
        set
        {
            lock (_lock)
            {
                _deviceId = value;
                if (_deviceId.HasValue)
                {
                    _lastPlaybackDeviceId = _deviceId;
                }
            }
        }
    }

    [MemberNotNullWhen(true, nameof(DeviceId))]
    public bool HasDevice => DeviceId.HasValue && DeviceId.Value != Guid.Empty;

    public Guid DeviceIdOrThrow => HasDevice
        ? DeviceId.Value
        : throw new InvalidRequestException(nameof(DeviceId), $"Playback State for {UserId} currently does not have a device");

    public bool Playing
    {
        get { lock (_lock) return _playing; }
    }

    public TimeSpan CurrentTime
    {
        get { lock (_lock) return _currentTime; }
    }

    public void UpdateWithoutEvents(IPlaybackSession session, NodePath nodePath, bool includePlaying)
    {
        if (session.UserId != UserId)
        {
            throw new InvalidRequestException(nameof(UserId),
                "Trying to update playback state with another session's state");
        }

        lock (_lock)
        {
            _sessionId = session.SessionId;
            _deviceId = session.DeviceId;
            _nodePath = nodePath;

            if (includePlaying)
            {
                _playing = session.Playing;
            }

            _currentTime = session.CurrentTime;
        }
    }

    public void SetWaitingForPause()
    {
        _pauseSemaphore.Reset();
        logger.LogWarning("Set Waiting For Pause IsSet: {IsSet}", _pauseSemaphore.IsSet);
    }

    public void WaitForPause()
    {
        var success = _pauseSemaphore.Wait(TimeSpan.FromSeconds(20));
        if (!success)
        {
            SetPlaying(Playing, CurrentTime);
        }
    }

    public void HandleDeviceDisconnected(Guid deviceId)
    {
        bool shouldRaise;

        lock (_lock)
        {
            if (!_deviceId.HasValue || _deviceId.Value != deviceId)
                return;

            _deviceId = null;
            _playing = false;
            shouldRaise = true;
        }

        _pauseSemaphore.Set();

        if (shouldRaise)
        {
            AudioPlayerStateChanged(AudioPlayerStateUpdateType.Playing);
        }
    }

    public void SetPlaying(bool playing, TimeSpan currentTime, bool raiseEvents = true)
    {
        bool changed;

        lock (_lock)
        {
            _playing = playing;
            _currentTime = currentTime;
            changed = raiseEvents;
        }

        if (!playing)
        {
            _pauseSemaphore.Set();
        }

        if (changed)
        {
            AudioPlayerStateChanged(AudioPlayerStateUpdateType.Playing);
        }
    }

    public void Seek(TimeSpan time)
    {
        lock (_lock)
        {
            _currentTime = time;
        }

        AudioPlayerStateChanged(AudioPlayerStateUpdateType.Seek);
    }

    public void UpdateAudioPlayerCurrentTime(Guid deviceId, TimeSpan currentTime)
    {
        bool changed;

        lock (_lock)
        {
            if (_deviceId != deviceId)
            {
                throw new InvalidRequestException(nameof(DeviceId),
                    $"Only the playing device can update state. Expected: {_deviceId}, got: {deviceId}");
            }

            if (!_playing)
            {
                throw new InvalidRequestException(nameof(Playing), "Cannot update playback state while paused");
            }

            _currentTime = currentTime;
            changed = true;
        }

        if (changed)
        {
            AudioPlayerStateChanged(AudioPlayerStateUpdateType.CurrentTime);
        }
    }

    public void ClearSession()
    {
        lock (_lock)
        {
            _sessionId = null;
            _nodePath = null;
            _currentTime = TimeSpan.Zero;
        }
    }

    private void AudioPlayerStateChanged(AudioPlayerStateUpdateType type)
    {
        try
        {
            AudioPlayerStateUpdated?.Invoke(this, type);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error during AudioPlayerStateUpdated event invocation");
        }
    }
}
