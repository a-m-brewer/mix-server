using MixServer.Domain.Users.Entities;

namespace MixServer.Domain.Users.Services;

public interface IDeviceTrackingService
{
    bool DeviceInteractedWith(Guid deviceId);
    void SetOnline(string userId, Guid deviceId, bool online);
    void SetInteraction(string userId, Guid deviceId, bool interactedWith);
    void UpdateCapabilities(string userId, Guid deviceId, Dictionary<string, bool> capabilities);
    void Populate(Device device);
    void Populate(List<Device> devices);
}