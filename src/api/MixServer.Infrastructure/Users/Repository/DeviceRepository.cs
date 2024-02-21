using Microsoft.EntityFrameworkCore;
using MixServer.Domain.Users.Entities;
using MixServer.Domain.Users.Repositories;
using MixServer.Infrastructure.EF;

namespace MixServer.Infrastructure.Users.Repository;

public class DeviceRepository(MixServerDbContext context) : IDeviceRepository
{
    public Task<Device?> SingleOrDefaultAsync(Guid deviceId)
    {
        return context.Devices.SingleOrDefaultAsync(d => d.Id == deviceId);
    }

    public async Task AddAsync(Device device)
    {
        await context.Devices.AddAsync(device);
    }

    public void Delete(Device device)
    {
        context.Devices.Remove(device);
    }
}