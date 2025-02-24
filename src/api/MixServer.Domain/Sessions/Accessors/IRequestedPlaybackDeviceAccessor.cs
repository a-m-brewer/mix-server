using MixServer.Domain.Users.Models;

namespace MixServer.Domain.Sessions.Accessors;

public interface IRequestedPlaybackDeviceAccessor
{ 
    IDeviceState PlaybackDevice { get; }
    IDeviceState RequestDevice { get; }
}