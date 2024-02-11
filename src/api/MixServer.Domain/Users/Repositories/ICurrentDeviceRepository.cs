using MixServer.Domain.Persistence;

namespace MixServer.Domain.Users.Repositories;

public interface ICurrentDeviceRepository : IScopedRepository
{
    Guid DeviceId { get; }
}