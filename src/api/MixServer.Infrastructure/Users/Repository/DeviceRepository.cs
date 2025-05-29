using Microsoft.EntityFrameworkCore;
using MixServer.Domain.Users.Entities;
using MixServer.Domain.Users.Repositories;
using MixServer.Infrastructure.EF;

namespace MixServer.Infrastructure.Users.Repository;

public class DeviceRepository(MixServerDbContext context) : IDeviceRepository
{
    public Task<Device?> SingleOrDefaultAsync(Guid deviceId, CancellationToken cancellationToken = default)
    {
        return context.Devices.SingleOrDefaultAsync(d => d.Id == deviceId, cancellationToken: cancellationToken);
    }

    public async Task AddAsync(Device device, CancellationToken cancellationToken = default)
    {
        await context.Devices.AddAsync(device, cancellationToken);
    }

    public void Delete(Device device)
    {
        context.Devices.Remove(device);
    }
}