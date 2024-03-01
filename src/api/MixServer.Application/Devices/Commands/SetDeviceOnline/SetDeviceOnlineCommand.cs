namespace MixServer.Application.Devices.Commands.SetDeviceOnline;

public class SetDeviceOnlineCommand(bool online)
{
    public bool Online { get; } = online;
}