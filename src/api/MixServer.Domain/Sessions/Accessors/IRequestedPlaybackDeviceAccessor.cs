using MixServer.Domain.Users.Models;

namespace MixServer.Domain.Sessions.Accessors;

public interface IRequestedPlaybackDeviceAccessor
{ 
    IDeviceState PlaybackDevice { get; }
    bool HasPlaybackDevice { get; }
    IDeviceState RequestDevice { get; }
}