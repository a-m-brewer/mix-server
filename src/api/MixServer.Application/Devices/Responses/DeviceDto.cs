using MixServer.Domain.Users.Enums;

namespace MixServer.Application.Devices.Responses;

public class DeviceDto
{
    public Guid Id { get; set; }
    public DateTime LastSeen { get; set; }
    public ClientType ClientType { get; set; }
    public DeviceType DeviceType { get; set; }
    public bool InteractedWith { get; set; }
    public Dictionary<string, bool> Capabilities { get; set; } = new();
    public bool Online { get; set; }
    public string? BrowserName { get; set; }
    public string? Model { get; set; }
    public string? Brand { get; set; }
    public string? OsName { get; set; }
    public string? OsVersion { get; set; }
}
