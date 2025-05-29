using MixServer.Domain.Persistence;
using MixServer.Domain.Users.Entities;

namespace MixServer.Domain.Users.Repositories;

public interface IDeviceRepository : ITransientRepository
{
    Task<Device?> SingleOrDefaultAsync(Guid deviceId, CancellationToken cancellationToken);
    Task AddAsync(Device device, CancellationToken cancellationToken);
    void Delete(Device device);
}