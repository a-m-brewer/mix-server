using MixServer.Domain.Persistence;
using MixServer.Shared.Interfaces;

namespace MixServer.Domain.Users.Repositories;

public interface ICurrentDeviceRepository : IScopedRepository
{
    Guid DeviceId { get; }
}