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

public class DeviceInfo : IDeviceInfo
{
    public DeviceInfo(
        ClientType clientType,
        DeviceType deviceType,
        string? browserName,
        string? model,
        string? brand,
        string? osName,
        string? osVersion)
    {
        ClientType = clientType;
        DeviceType = deviceType;
        BrowserName = browserName;
        Model = model;
        Brand = brand;
        OsName = osName;
        OsVersion = osVersion;
    }

    public ClientType ClientType { get; }
    public DeviceType DeviceType { get; }
    public string? BrowserName { get; }
    public string? Model { get; }
    public string? Brand { get; }
    public string? OsName { get; }
    public string? OsVersion { get; }

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