namespace MixServer.Application.Devices.Commands.UpdateDevicePlaybackCapabilities;

public class UpdateDevicePlaybackCapabilitiesCommand
{
    public Dictionary<string, bool> Capabilities { get; set; } = new();
}