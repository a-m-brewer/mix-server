using MixServer.Domain.Users.Models;
using MixServer.Domain.Users.Repositories;

namespace MixServer.Services;

public class DeviceDetectionBackgroundService(
    IDeviceInfoChannel channel,
    IServiceProvider serviceProvider,
    ILogger<DeviceDetectionBackgroundService> logger)
    : ChannelBackgroundService<DeviceInfoRequest>(channel, serviceProvider, logger);