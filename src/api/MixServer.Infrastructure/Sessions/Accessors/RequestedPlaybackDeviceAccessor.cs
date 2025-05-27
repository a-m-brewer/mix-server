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
    public IDeviceState PlaybackDevice => deviceTrackingService.GetDeviceStateOrThrow(GetPlaybackDeviceId());

    public bool HasPlaybackDevice => deviceTrackingService.HasDeviceState(GetPlaybackDeviceId());

    public IDeviceState RequestDevice => deviceTrackingService.GetDeviceStateOrThrow(currentDeviceRepository.DeviceId);

    private Guid GetPlaybackDeviceId()
    {
        var user = currentUserRepository.CurrentUser;
        
        var requestedDeviceId = playbackTrackingService.TryGet(user.Id, out var state) && state.HasDevice
            ? state.DeviceIdOrThrow
            : currentDeviceRepository.DeviceId;
        
        return requestedDeviceId;
    }
}