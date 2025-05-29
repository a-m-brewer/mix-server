using MixServer.Domain.Users.Models;

namespace MixServer.Domain.Sessions.Accessors;

public interface IRequestedPlaybackDeviceAccessor
{
    Task<IDeviceState> GetPlaybackDeviceAsync();
    
    Task<bool> HasPlaybackDeviceAsync();
}