using MixServer.Domain.Users.Models;

namespace MixServer.Domain.Sessions.Accessors;

public interface IRequestedPlaybackDeviceAccessor
{ 
    IDeviceState DeviceState { get; }
}