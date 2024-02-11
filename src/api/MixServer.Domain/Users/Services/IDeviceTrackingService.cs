using MixServer.Domain.Users.Entities;

namespace MixServer.Domain.Users.Services;

public interface IDeviceTrackingService
{
    bool DeviceInteractedWith(Guid deviceId);
    void SetInteraction(string userId, Guid deviceId, bool interactedWith);
    void Populate(Device device);
    void Populate(List<Device> devices);
}