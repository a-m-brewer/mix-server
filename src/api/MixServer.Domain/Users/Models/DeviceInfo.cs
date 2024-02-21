using MixServer.Domain.Users.Enums;

namespace MixServer.Domain.Users.Models;

public interface IDeviceInfo
{
    ClientType ClientType { get; }
    
    DeviceType DeviceType { get; }
    
    string? BrowserName { get; }
    
    string? Model { get; }

    string? Brand { get; }
    
    string? OsName { get; }
    
    string? OsVersion { get; }
}

public class DeviceInfo(
    ClientType clientType,
    DeviceType deviceType,
    string? browserName,
    string? model,
    string? brand,
    string? osName,
    string? osVersion)
    : IDeviceInfo
{
    public ClientType ClientType { get; } = clientType;
    public DeviceType DeviceType { get; } = deviceType;
    public string? BrowserName { get; } = browserName;
    public string? Model { get; } = model;
    public string? Brand { get; } = brand;
    public string? OsName { get; } = osName;
    public string? OsVersion { get; } = osVersion;

    public static IDeviceInfo Default =>
        new DeviceInfo(
            ClientType.Unknown,
            DeviceType.Unknown,
            null,
            null,
            null,
            null,
            null
        );
}