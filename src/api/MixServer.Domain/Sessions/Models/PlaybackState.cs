using Microsoft.Extensions.Logging;
using MixServer.Domain.Exceptions;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Models.Metadata;
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
    private Guid? _deviceId = session.DeviceId;

    public event EventHandler<AudioPlayerStateUpdateType>? AudioPlayerStateUpdated;
    
    public string UserId { get; } = session.UserId;

    public Guid? SessionId { get; private set; } = session.SessionId;

    public required NodePath? NodePath { get; set; }

    public Guid? LastPlaybackDeviceId { get; private set; } = session.LastPlaybackDeviceId;

    public Guid? DeviceId
    {
        get => _deviceId;
        set
        {
            _deviceId = value;
            if (_deviceId.HasValue)
            {
                LastPlaybackDeviceId = _deviceId;
            }
        }
    }

    public bool HasDevice => DeviceId.HasValue && DeviceId.Value != Guid.Empty;
    
    public Guid DeviceIdOrThrow => HasDevice
        ? DeviceId!.Value
        : throw new InvalidRequestException(nameof(DeviceId),
            $"Playback State for {UserId} currently does not have a device");

    public bool Playing { get; private set; } = session.Playing;

    public TimeSpan CurrentTime { get; private set; } = session.CurrentTime;

    public void UpdateWithoutEvents(IPlaybackSession session, NodePath nodePath, bool includePlaying)
    {
        if (session.UserId != UserId)
        {
            throw new InvalidRequestException(nameof(UserId),
                "Trying to update playback state with another sessions state");
        }
        
        SessionId = session.SessionId;
        DeviceId = session.DeviceId;
        NodePath = nodePath;

        if (includePlaying)
        {
            SetPlaying(session.Playing, session.CurrentTime, false);
        }
        else
        {
            CurrentTime = session.CurrentTime;
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

        if (success)
        {
            return;
        }

        SetPlaying(Playing, CurrentTime);
    }

    public void HandleDeviceDisconnected(Guid deviceId)
    {
        if (!DeviceId.HasValue || DeviceId.Value != deviceId)
        {
            return;
        }

        DeviceId = null;
        SetPlaying(false, CurrentTime);
    }
    
    public void SetPlaying(bool playing, TimeSpan currentTime, bool raiseEvents = true)
    {
        if (!playing)
        {
            _pauseSemaphore.Set();
        }

        Playing = playing;
        CurrentTime = currentTime;

        if (raiseEvents)
        {
            AudioPlayerStateChanged(AudioPlayerStateUpdateType.Playing);
        }
    }

    public void Seek(TimeSpan time)
    {
        CurrentTime = time;

        AudioPlayerStateChanged(AudioPlayerStateUpdateType.Seek);
    }

    public void UpdateAudioPlayerCurrentTime(Guid deviceId, TimeSpan currentTime)
    {
        AssertPlayingDeviceUpdatedState(deviceId);

        if (!Playing)
        {
            throw new InvalidRequestException(nameof(Playing), "Can not update playback state whilst paused");
        }

        CurrentTime = currentTime;

        AudioPlayerStateChanged(AudioPlayerStateUpdateType.CurrentTime);
    }

    private void AssertPlayingDeviceUpdatedState(Guid deviceId)
    {
        if (deviceId != DeviceId)
        {
            throw new InvalidRequestException(nameof(DeviceId),
                $"Only playing device can set paused state Playing: {DeviceId} Requester: {deviceId}");
        }
    }

    private void AudioPlayerStateChanged(AudioPlayerStateUpdateType type)
    {
        AudioPlayerStateUpdated?.Invoke(this, type);
    }

    public void ClearSession()
    {
        SessionId = null;
        NodePath = null;
        CurrentTime = TimeSpan.Zero;
    }
}