using MixServer.Application.Users.Responses;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Users.Entities;
using MixServer.Domain.Users.Models;

namespace MixServer.Application.Users.Converters;

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
            InteractedWith = value.InteractedWith
        };
    }
}