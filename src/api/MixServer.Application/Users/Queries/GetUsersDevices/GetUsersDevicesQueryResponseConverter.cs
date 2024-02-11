using MixServer.Application.Users.Responses;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Users.Entities;

namespace MixServer.Application.Users.Queries.GetUsersDevices;

public class GetUsersDevicesQueryResponseConverter
    : IConverter<List<IDevice>, GetUsersDevicesQueryResponse>
{
    private readonly IConverter<IDevice, DeviceDto> _deviceDtoConverter;

    public GetUsersDevicesQueryResponseConverter(
        IConverter<IDevice, DeviceDto> deviceDtoConverter)
    {
        _deviceDtoConverter = deviceDtoConverter;
    }
    
    public GetUsersDevicesQueryResponse Convert(List<IDevice> value)
    {
        return new GetUsersDevicesQueryResponse
        {
            Devices = value.Select(_deviceDtoConverter.Convert).ToList()
        };
    }
}