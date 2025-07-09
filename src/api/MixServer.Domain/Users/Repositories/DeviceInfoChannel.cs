using Microsoft.Extensions.Logging;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Users.Models;

namespace MixServer.Domain.Users.Repositories;

public interface IDeviceInfoChannel : IChannel<DeviceInfoRequest>;

public class DeviceInfoChannelBase(ILogger<DeviceInfoChannelBase> logger) : ChannelBase<DeviceInfoRequest>(logger), IDeviceInfoChannel;