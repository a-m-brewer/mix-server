using Microsoft.Extensions.Logging;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Persistence;
using MixServer.Domain.Users.Models;
using MixServer.Domain.Users.Repositories;
using MixServer.Domain.Users.Services;
using MixServer.Domain.Utilities;

namespace MixServer.Application.Devices.Commands.DetectDevice;

public class DetectDeviceCommandHandler(
    IDateTimeProvider dateTimeProvider,
    IDeviceDetectionService deviceDetectionService,
    IDeviceRepository deviceRepository,
    IDeviceTrackingService deviceTrackingService,
    ILogger<DetectDeviceCommandHandler> logger,
    IUnitOfWork unitOfWork) : ICommandHandler<DeviceInfoRequest>
{
    public async Task HandleAsync(DeviceInfoRequest request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Populating Device Info for device: {DeviceId}", request.DeviceId);
        
        var device = await deviceRepository.SingleOrDefaultAsync(request.DeviceId, cancellationToken);

        if (device == null)
        {
            logger.LogWarning("Failed to populate device: {DeviceId} as it could not be found", request.DeviceId);
            return;
        }
        
        device.LastSeen = dateTimeProvider.UtcNow;
        
        try
        {
            logger.LogInformation("Fetching device info for device: {DeviceId}", request.DeviceId);
            var deviceInfo = deviceDetectionService.GetCurrentUsersDevice(request.Headers);
            logger.LogInformation("Device info fetched for device: {DeviceId}", request.DeviceId);

            device.UpdateDeviceInfo(deviceInfo);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to set deviceInfo");
        }
        
        deviceTrackingService.Populate(device);

        unitOfWork.InvokeCallbackOnSaved(cb =>
        {
            logger.LogInformation("Sending device updated message for device: {DeviceId}", request.DeviceId);
            return cb.DeviceUpdated(device);
        });
        
        await unitOfWork.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Finished fetching device info for device: {DeviceId}", request.DeviceId);
    }
}