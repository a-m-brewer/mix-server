using Microsoft.EntityFrameworkCore;
using MixServer.Domain.Users.Entities;
using MixServer.Domain.Users.Repositories;
using MixServer.Infrastructure.EF;

namespace MixServer.Infrastructure.Users.Repository;

public class DeviceRepository : IDeviceRepository
{
    private readonly MixServerDbContext _context;

    public DeviceRepository(MixServerDbContext context)
    {
        _context = context;
    }
    
    public Task<Device?> SingleOrDefaultAsync(Guid deviceId)
    {
        return _context.Devices.SingleOrDefaultAsync(d => d.Id == deviceId);
    }

    public async Task AddAsync(Device device)
    {
        await _context.Devices.AddAsync(device);
    }

    public void Delete(Device device)
    {
        _context.Devices.Remove(device);
    }
}