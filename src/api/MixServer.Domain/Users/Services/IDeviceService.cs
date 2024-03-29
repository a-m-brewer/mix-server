using MixServer.Domain.Users.Entities;

namespace MixServer.Domain.Users.Services;

public interface IDeviceService
{
    Task<List<IDevice>> GetUsersDevicesAsync();
    Task<Device> GetOrAddAsync(Guid? requestDeviceId);
    Task<Device?> SingleOrDefaultAsync(Guid deviceId);
    void UpdateDevice(Device device);
    Task DeleteDeviceAsync(Guid deviceId);
}