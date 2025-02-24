using MixServer.Domain.Users.Entities;
using MixServer.Domain.Users.Models;

namespace MixServer.Domain.Users.Services;

public interface IDeviceTrackingService
{
    IDeviceState GetDeviceStateOrThrow(Guid deviceId);
    void SetOnline(string userId, Guid deviceId, bool online);
    void SetInteraction(string userId, Guid deviceId, bool interactedWith);
    void UpdateCapabilities(string userId, Guid deviceId, Dictionary<string, bool> capabilities);
    void Populate(Device device);
    void Populate(List<Device> devices);
}