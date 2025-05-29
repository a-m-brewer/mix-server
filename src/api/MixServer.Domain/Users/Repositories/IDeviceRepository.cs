using MixServer.Domain.Persistence;
using MixServer.Domain.Users.Entities;

namespace MixServer.Domain.Users.Repositories;

public interface IDeviceRepository : ITransientRepository
{
    Task<Device?> SingleOrDefaultAsync(Guid deviceId, CancellationToken cancellationToken = default);
    Task AddAsync(Device device, CancellationToken cancellationToken = default);
    void Delete(Device device);
}