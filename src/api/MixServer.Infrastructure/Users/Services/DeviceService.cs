using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using MixServer.Domain.Exceptions;
using MixServer.Domain.Persistence;
using MixServer.Domain.Users.Entities;
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
    IServiceProvider serviceProvider,
    IUnitOfWork unitOfWork)
    : IDeviceService
{
    public async Task<List<IDevice>> GetUsersDevicesAsync()
    {
        await currentUserRepository.LoadAllDevicesAsync();

        PopulateCurrentUserDevices();

        return currentUserRepository.CurrentUser.Devices
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
        
        // Don't want this code to slow down login / refresh
        var headers = httpContextAccessor.HttpContext?.Request.Headers
            .ToDictionary(k => k.Key, v => v.Value);
        if (headers != null)
        {
            Task.Run(() => PopulateDeviceInfoAsync(device.Id, headers));
        }
        
        Populate(device);
        
        unitOfWork.InvokeCallbackOnSaved(service => service.DeviceUpdated(device));
    }

    public async Task DeleteDeviceAsync(Guid deviceId)
    {
        await currentUserRepository.LoadDeviceByIdAsync(deviceId);

        var device = currentUserRepository.CurrentUser.Devices.SingleOrDefault(s => s.Id == deviceId);

        if (device == null)
        {
            throw new NotFoundException(nameof(Device), deviceId);
        }
        
        deviceRepository.Delete(device);

        unitOfWork.InvokeCallbackOnSaved(s => s.DeviceDeleted(currentUserRepository.CurrentUser.Id, deviceId));
    }

    private void Populate(Device device)
    {
        deviceTrackingService.Populate(device);
    }

    private void PopulateCurrentUserDevices()
    {
        deviceTrackingService.Populate(currentUserRepository.CurrentUser.Devices);
    }

    private async Task PopulateDeviceInfoAsync(
        Guid deviceId,
        IDictionary<string, StringValues> headers)
    {
        using var scope = serviceProvider.CreateScope();
        var deviceDetectionService = scope.ServiceProvider.GetRequiredService<IDeviceDetectionService>();
        var scopedDeviceRepository = scope.ServiceProvider.GetRequiredService<IDeviceRepository>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<DeviceService>>();
        var scopedUnitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        
        logger.LogInformation("Populating Device Info for device: {DeviceId}", deviceId);
        
        var device = await scopedDeviceRepository.SingleOrDefaultAsync(deviceId);

        if (device == null)
        {
            logger.LogWarning("Failed to populate device: {DeviceId} as it could not be found", deviceId);
            return;
        }
        
        device.LastSeen = dateTimeProvider.UtcNow;
        
        try
        {
            logger.LogInformation("Fetching device info for device: {DeviceId}", deviceId);
            var deviceInfo = deviceDetectionService.GetCurrentUsersDevice(headers);
            logger.LogInformation("Device info fetched for device: {DeviceId}", deviceId);

            device.UpdateDeviceInfo(deviceInfo);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to set deviceInfo");
        }
        
        Populate(device);

        scopedUnitOfWork.InvokeCallbackOnSaved(cb =>
        {
            logger.LogInformation("Sending device updated message for device: {DeviceId}", deviceId);
            return cb.DeviceUpdated(device);
        });
        
        await scopedUnitOfWork.SaveChangesAsync();
        logger.LogInformation("Finished fetching device info for device: {DeviceId}", deviceId);
    }
}