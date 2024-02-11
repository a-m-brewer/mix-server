using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using MixServer.Domain.Callbacks;
using MixServer.Domain.Exceptions;
using MixServer.Domain.Persistence;
using MixServer.Domain.Sessions.Services;
using MixServer.Domain.Users.Entities;
using MixServer.Domain.Users.Repositories;
using MixServer.Domain.Users.Services;
using MixServer.Domain.Utilities;
using MixServer.Infrastructure.Users.Repository;

namespace MixServer.Infrastructure.Users.Services;

public class DeviceService : IDeviceService
{
    private readonly ICurrentUserRepository _currentUserRepository;
    private readonly ICallbackService _callbackService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IDeviceRepository _deviceRepository;
    private readonly IDeviceTrackingService _deviceTrackingService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IPlaybackTrackingService _playbackTrackingService;
    private readonly IServiceProvider _serviceProvider;

    public DeviceService(
        ICurrentUserRepository currentUserRepository,
        ICallbackService callbackService,
        IDateTimeProvider dateTimeProvider,
        IDeviceRepository deviceRepository,
        IDeviceTrackingService deviceTrackingService,
        IHttpContextAccessor httpContextAccessor,
        IPlaybackTrackingService playbackTrackingService,
        IServiceProvider serviceProvider)
    {
        _currentUserRepository = currentUserRepository;
        _callbackService = callbackService;
        _dateTimeProvider = dateTimeProvider;
        _deviceRepository = deviceRepository;
        _deviceTrackingService = deviceTrackingService;
        _httpContextAccessor = httpContextAccessor;
        _playbackTrackingService = playbackTrackingService;
        _serviceProvider = serviceProvider;
    }

    public async Task<List<IDevice>> GetUsersDevicesAsync()
    {
        await _currentUserRepository.LoadAllDevicesAsync();

        PopulateCurrentUserDevices();

        return _currentUserRepository.CurrentUser.Devices
            .Cast<IDevice>()
            .ToList();
    }

    public async Task<Device> GetOrAddAsync(Guid? requestDeviceId)
    {
        var device = requestDeviceId.HasValue
            ? await _deviceRepository.SingleOrDefaultAsync(requestDeviceId.Value)
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
        
        await _deviceRepository.AddAsync(device);

        return device;
    }

    public async Task<Device?> SingleOrDefaultAsync(Guid deviceId)
    {
        var device = await _deviceRepository.SingleOrDefaultAsync(deviceId);

        if (device == null)
        {
            return null;
        }
        
        Populate(device);

        return device;
    }

    public void UpdateDevice(Device device)
    {
        device.LastSeen = _dateTimeProvider.UtcNow;
        
        // Don't want this code to slow down login / refresh
        var headers = _httpContextAccessor.HttpContext?.Request.Headers
            .ToDictionary(k => k.Key, v => v.Value);
        if (headers != null)
        {
            Task.Run(() => PopulateDeviceInfoAsync(device.Id, headers));
        }
        
        Populate(device);
        
        _callbackService.InvokeCallbackOnSaved(service => service.DeviceUpdated(device));
    }

    public async Task DeleteDeviceAsync(Guid deviceId)
    {
        await _currentUserRepository.LoadDeviceByIdAsync(deviceId);

        var device = _currentUserRepository.CurrentUser.Devices.SingleOrDefault(s => s.Id == deviceId);

        if (device == null)
        {
            throw new NotFoundException(nameof(Device), deviceId);
        }
        
        _deviceRepository.Delete(device);

        _callbackService.InvokeCallbackOnSaved(s => s.DeviceDeleted(_currentUserRepository.CurrentUser.Id, deviceId));
    }

    private void Populate(Device device)
    {
        _deviceTrackingService.Populate(device);
    }

    private void PopulateCurrentUserDevices()
    {
        _deviceTrackingService.Populate(_currentUserRepository.CurrentUser.Devices);
    }

    private async Task PopulateDeviceInfoAsync(
        Guid deviceId,
        IDictionary<string, StringValues> headers)
    {

        using var scope = _serviceProvider.CreateScope();
        var callbackService = scope.ServiceProvider.GetRequiredService<ICallbackService>();
        var deviceDetectionService = scope.ServiceProvider.GetRequiredService<IDeviceDetectionService>();
        var deviceRepository = scope.ServiceProvider.GetRequiredService<IDeviceRepository>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<DeviceService>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        
        logger.LogInformation("Populating Device Info for device: {DeviceId}", deviceId);
        
        var device = await deviceRepository.SingleOrDefaultAsync(deviceId);

        if (device == null)
        {
            logger.LogWarning("Failed to populate device: {DeviceId} as it could not be found", deviceId);
            return;
        }
        
        device.LastSeen = _dateTimeProvider.UtcNow;
        
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

        callbackService.InvokeCallbackOnSaved(cb =>
        {
            logger.LogInformation("Sending device updated message for device: {DeviceId}", deviceId);
            return cb.DeviceUpdated(device);
        });
        
        await unitOfWork.SaveChangesAsync();
        logger.LogInformation("Finished fetching device info for device: {DeviceId}", deviceId);
    }
}