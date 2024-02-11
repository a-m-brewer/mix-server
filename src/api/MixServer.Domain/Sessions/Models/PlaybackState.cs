using Microsoft.Extensions.Logging;
using MixServer.Domain.Exceptions;
using MixServer.Domain.Sessions.Entities;
using MixServer.Domain.Sessions.Enums;

namespace MixServer.Domain.Sessions.Models;

public interface IPlaybackState
{
    string UserId { get; }
    Guid? SessionId { get; }
    Guid? DeviceId { get; }
    bool Playing { get; }
    TimeSpan CurrentTime { get; }
}

public class PlaybackState : IPlaybackState
{
    private readonly ILogger<PlaybackState> _logger;
    private readonly ManualResetEventSlim _pauseSemaphore = new(true);

    public PlaybackState(IPlaybackState session, ILogger<PlaybackState> logger)
    {
        _logger = logger;
        SessionId = session.SessionId;
        DeviceId = session.DeviceId;
        CurrentTime = session.CurrentTime;
        Playing = session.Playing;
        UserId = session.UserId;
    }

    public event EventHandler<AudioPlayerStateUpdateType>? AudioPlayerStateUpdated;
    
    public string UserId { get; }

    public Guid? SessionId { get; private set; }

    public Guid? DeviceId { get; set; }

    public bool HasDevice => DeviceId.HasValue && DeviceId.Value != Guid.Empty;
    
    public Guid DeviceIdOrThrow => HasDevice
        ? DeviceId!.Value
        : throw new InvalidRequestException(nameof(DeviceId),
            $"Playback State for {UserId} currently does not have a device");

    public bool Playing { get; private set; }

    public TimeSpan CurrentTime { get; private set; }

    public void UpdateWithoutEvents(IPlaybackSession session, bool includePlaying)
    {
        if (session.UserId != UserId)
        {
            throw new InvalidRequestException(nameof(UserId),
                "Trying to update playback state with another sessions state");
        }
        
        SessionId = session.SessionId;
        DeviceId = session.DeviceId;

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
        
        _logger.LogWarning("Set Waiting For Pause IsSet: {IsSet}", _pauseSemaphore.IsSet);
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

    public void UpdateAudioPlayerState(Guid deviceId, TimeSpan currentTime)
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
        CurrentTime = TimeSpan.Zero;
    }
}