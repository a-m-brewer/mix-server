using MixServer.Application.Devices.Responses;

namespace MixServer.Application.Devices.Queries.GetUsersDevices;

public class GetUsersDevicesQueryResponse
{
    public List<DeviceDto> Devices { get; set; } = [];
}