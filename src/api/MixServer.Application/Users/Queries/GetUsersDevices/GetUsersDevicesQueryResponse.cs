using MixServer.Application.Users.Responses;

namespace MixServer.Application.Users.Queries.GetUsersDevices;

public class GetUsersDevicesQueryResponse
{
    public List<DeviceDto> Devices { get; set; } = [];
}