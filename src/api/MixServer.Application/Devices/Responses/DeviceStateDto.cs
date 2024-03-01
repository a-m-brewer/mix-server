namespace MixServer.Application.Devices.Responses;

public class DeviceStateDto
{
    public Guid DeviceId { get; set; }

    public string LastInteractedWith { get; set; } = string.Empty;

    public bool InteractedWith { get; set; }
    
    public bool Online { get; set; }
}