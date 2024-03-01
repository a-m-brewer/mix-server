using MixServer.Application.Devices.Responses;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Users.Entities;

namespace MixServer.Application.Devices.Queries.GetUsersDevices;

public class GetUsersDevicesQueryResponseConverter(IConverter<IDevice, DeviceDto> deviceDtoConverter)
    : IConverter<List<IDevice>, GetUsersDevicesQueryResponse>
{
    public GetUsersDevicesQueryResponse Convert(List<IDevice> value)
    {
        return new GetUsersDevicesQueryResponse
        {
            Devices = value.Select(deviceDtoConverter.Convert).ToList()
        };
    }
}