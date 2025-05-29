using MixServer.Domain.Sessions.Accessors;
using MixServer.Domain.Sessions.Services;
using MixServer.Domain.Users.Models;
using MixServer.Domain.Users.Repositories;
using MixServer.Domain.Users.Services;
using MixServer.Infrastructure.Users.Repository;

namespace MixServer.Infrastructure.Sessions.Accessors;

public class RequestedPlaybackDeviceAccessor(
    ICurrentUserRepository currentUserRepository,
    IPlaybackTrackingService playbackTrackingService,
    ICurrentDeviceRepository currentDeviceRepository,
    IDeviceTrackingService deviceTrackingService) : IRequestedPlaybackDeviceAccessor
{
    public async Task<IDeviceState> GetPlaybackDeviceAsync()
    {
        return deviceTrackingService.GetDeviceStateOrThrow(await GetPlaybackDeviceId());
    }

    public async Task<bool> HasPlaybackDeviceAsync()
    {
        return deviceTrackingService.HasDeviceState(await GetPlaybackDeviceId());
    }

    private async Task<Guid> GetPlaybackDeviceId()
    {
        var user = await currentUserRepository.GetCurrentUserAsync();
        
        var requestedDeviceId = playbackTrackingService.TryGet(user.Id, out var state) && state.HasDevice
            ? state.DeviceIdOrThrow
            : currentDeviceRepository.DeviceId;
        
        return requestedDeviceId;
    }
}