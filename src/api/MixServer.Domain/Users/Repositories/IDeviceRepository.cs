using MixServer.Domain.Persistence;
using MixServer.Domain.Users.Entities;
using MixServer.Shared.Interfaces;

namespace MixServer.Domain.Users.Repositories;

public interface IDeviceRepository : ITransientRepository
{
    Task<Device?> SingleOrDefaultAsync(Guid deviceId);
    Task AddAsync(Device device);
    void Delete(Device device);
}