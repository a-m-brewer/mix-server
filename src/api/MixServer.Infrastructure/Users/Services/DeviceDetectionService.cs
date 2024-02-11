using DeviceDetectorNET;
using Microsoft.Extensions.Primitives;
using MixServer.Domain.Users.Models;
using MixServer.Domain.Users.Services;
using MixServer.Infrastructure.Users.Repository;

namespace MixServer.Infrastructure.Users.Services;

public class DeviceDetectionService : IDeviceDetectionService
{
    private readonly IDeviceCacheRepository _deviceCacheRepository;

    public DeviceDetectionService(
        IDeviceCacheRepository deviceCacheRepository)
    {
        _deviceCacheRepository = deviceCacheRepository;
    }

    public IDeviceInfo GetCurrentUsersDevice(IDictionary<string, StringValues> headers)
    {
        var userAgent = headers["User-Agent"];
        var otherHeaders = headers.ToDictionary(a => a.Key, a => a.Value.ToArray().FirstOrDefault());
        
        var clientHints = ClientHints.Factory(otherHeaders);

        var dd = new DeviceDetectorWrapper(userAgent, clientHints);
        
        dd.SetCache(_deviceCacheRepository.Cache);
        
        dd.DiscardBotInformation();
        dd.SkipBotDetection();

        dd.Parse();

        var clientInfo = dd.GetBrowserClient();
        var osInfo = dd.GetOs();
        var brand = dd.GetBrandName();
        var model = dd.GetModel();

        return new DeviceInfo(
            dd.ClientType,
            dd.DeviceType,
            clientInfo.Success ? clientInfo.Match.Name : null,
            string.IsNullOrWhiteSpace(model) ? null : model,
            string.IsNullOrWhiteSpace(brand) ? null : brand,
            osInfo.Success ? osInfo.Match.Name : null,
            osInfo.Success ? osInfo.Match.Version : null);
    }
}