using MixServer.Application.Devices.Responses;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Users.Entities;
using MixServer.Domain.Users.Models;

namespace MixServer.Application.Devices.Converters;

public class DeviceDtoConverter : 
    IConverter<IDevice, DeviceDto>,
    IConverter<IDeviceState, DeviceStateDto>
{
    public DeviceDto Convert(IDevice value)
    {
        return new DeviceDto
        {
            Id = value.Id,
            LastSeen = value.LastSeen,
            InteractedWith = value.InteractedWith,
            Capabilities = value.Capabilities.ToDictionary(),
            Online = value.Online,
            ClientType = value.ClientType,
            DeviceType = value.DeviceType,
            BrowserName = value.BrowserName,
            Model = value.Model,
            Brand = value.Brand,
            OsName = value.OsName,
            OsVersion = value.OsVersion
        };
    }

    public DeviceStateDto Convert(IDeviceState value)
    {
        return new DeviceStateDto
        {
            DeviceId = value.DeviceId,
            LastInteractedWith = value.LastInteractedWith,
            InteractedWith = value.InteractedWith,
            Online = value.Online,
            Capabilities = value.Capabilities.ToDictionary()
        };
    }
}