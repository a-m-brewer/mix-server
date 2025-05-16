using DeviceDetectorNET.Cache;
using MixServer.Domain.Persistence;
using MixServer.Shared.Interfaces;

namespace MixServer.Infrastructure.Users.Repository;

public interface IDeviceCacheRepository : ISingletonRepository
{
    DictionaryCache Cache { get; }
}

public class DeviceCacheRepository : IDeviceCacheRepository
{
    public DictionaryCache Cache { get; } = new();
}