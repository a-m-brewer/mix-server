using System.ComponentModel.DataAnnotations.Schema;
using MixServer.Domain.Users.Enums;
using MixServer.Domain.Users.Models;

namespace MixServer.Domain.Users.Entities;

public interface IDevice : IDeviceInfo, IDeviceState
{
    Guid Id { get; }
    DateTime LastSeen { get; }
}

public class Device : IDevice
{
    public Guid Id { get; set; }
    public DateTime LastSeen { get; set; }

    public List<UserCredential> UserCredentials { get; set; } = [];
    
    [NotMapped]
    public Guid DeviceId => Id;

    [NotMapped]
    public string LastInteractedWith { get; private set; } = string.Empty;

    [NotMapped]
    public bool InteractedWith { get; private set; }

    [NotMapped]
    public bool Online { get; set; }

    public void Populate(IDeviceState state)
    {
        Online = state.Online;
        LastInteractedWith = state.LastInteractedWith;
        InteractedWith = state.InteractedWith;
    }

    #region DeviceInfo

    public ClientType ClientType { get; set; }
    public DeviceType DeviceType { get; set; }
    public string? BrowserName { get; set; }
    public string? Model { get; set; }
    public string? Brand { get; set; }
    public string? OsName { get; set; }
    public string? OsVersion { get; set; }

    #endregion

    public void UpdateDeviceInfo(IDeviceInfo deviceInfo)
    {
        ClientType = deviceInfo.ClientType;
        DeviceType = deviceInfo.DeviceType;
        BrowserName = deviceInfo.BrowserName;
        Model = deviceInfo.Model;
        Brand = deviceInfo.Brand;
        OsName = deviceInfo.OsName;
        OsVersion = deviceInfo.OsVersion;
    }
}