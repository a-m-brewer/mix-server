using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using MixServer.Domain.Exceptions;
using MixServer.Domain.Persistence;
using MixServer.Domain.Users.Entities;
using MixServer.Domain.Users.Models;
using MixServer.Domain.Users.Repositories;
using MixServer.Domain.Users.Services;
using MixServer.Domain.Utilities;
using MixServer.Infrastructure.Users.Repository;

namespace MixServer.Infrastructure.Users.Services;

public class DeviceService(
    ICurrentUserRepository currentUserRepository,
    IDateTimeProvider dateTimeProvider,
    IDeviceRepository deviceRepository,
    IDeviceTrackingService deviceTrackingService,
    IHttpContextAccessor httpContextAccessor,
    IDeviceInfoChannel deviceInfoChannel,
    IUnitOfWork unitOfWork)
    : IDeviceService
{
    public async Task<List<IDevice>> GetUsersDevicesAsync()
    {
        await currentUserRepository.LoadAllDevicesAsync();

        await PopulateCurrentUserDevicesAsync();
        
        var user = await currentUserRepository.GetCurrentUserAsync();

        return user.Devices
            .Cast<IDevice>()
            .ToList();
    }

    public async Task<Device> GetOrAddAsync(Guid? requestDeviceId)
    {
        var device = requestDeviceId.HasValue
            ? await deviceRepository.SingleOrDefaultAsync(requestDeviceId.Value)
            : null;

        if (device != null)
        {
            return device;
        }

        device = new Device
        {
            Id = requestDeviceId ?? Guid.NewGuid(),
        };

        Populate(device);
        
        await deviceRepository.AddAsync(device);

        return device;
    }

    public async Task<Device?> SingleOrDefaultAsync(Guid deviceId)
    {
        var device = await deviceRepository.SingleOrDefaultAsync(deviceId);

        if (device == null)
        {
            return null;
        }
        
        Populate(device);

        return device;
    }

    public void UpdateDevice(Device device)
    {
        device.LastSeen = dateTimeProvider.UtcNow;
        
        Populate(device);
        
        unitOfWork.InvokeCallbackOnSaved(service => service.DeviceUpdated(device));
        
        var headers = httpContextAccessor.HttpContext?.Request.Headers
            .ToDictionary(k => k.Key, v => v.Value);
        if (headers != null)
        {
            unitOfWork.OnSaved(() => deviceInfoChannel.WriteAsync(new DeviceInfoRequest
            {
                DeviceId = device.Id,
                Headers = headers
            }));
        }
    }

    public async Task DeleteDeviceAsync(Guid deviceId)
    {
        await currentUserRepository.LoadDeviceByIdAsync(deviceId);

        var user = await currentUserRepository.GetCurrentUserAsync();
        var device = user.Devices.SingleOrDefault(s => s.Id == deviceId);

        if (device == null)
        {
            throw new NotFoundException(nameof(Device), deviceId);
        }
        
        deviceRepository.Delete(device);

        unitOfWork.InvokeCallbackOnSaved(async s => await s.DeviceDeleted((await currentUserRepository.GetCurrentUserAsync()).Id, deviceId));
    }

    private void Populate(Device device)
    {
        deviceTrackingService.Populate(device);
    }

    private async Task PopulateCurrentUserDevicesAsync()
    {
        deviceTrackingService.Populate((await currentUserRepository.GetCurrentUserAsync()).Devices);
    }
}