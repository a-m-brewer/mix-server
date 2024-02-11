namespace MixServer.Domain.Users.Services;

public interface IConnectionManager
{
    bool DeviceConnected(Guid deviceId);
}