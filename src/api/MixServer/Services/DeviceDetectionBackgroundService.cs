using MixServer.Domain.Users.Models;
using MixServer.Domain.Users.Repositories;
using MixServer.Infrastructure.Users.Services;

namespace MixServer.Services;

public class DeviceDetectionBackgroundService(
    IDeviceInfoChannel channel,
    IServiceProvider serviceProvider,
    ILogger<DeviceDetectionBackgroundService> logger)
    : ChannelBackgroundService<DeviceInfoRequest>(channel, serviceProvider, logger)
{
    protected override async Task ProcessRequestAsync(DeviceInfoRequest request, IServiceProvider serviceProvider, CancellationToken stoppingToken)
    {
        var service = serviceProvider.GetRequiredService<IDeviceDetectionPersistenceService>();
        await service.DetectAndPersistDeviceAsync(request, stoppingToken);
    }
}