using MixServer.Domain.Interfaces;
using MixServer.Domain.Users.Entities;
using MixServer.Domain.Users.Services;

namespace MixServer.Application.Devices.Queries.GetUsersDevices;

public class GetUsersDevicesQueryHandler(
    IDeviceService deviceService,
    IConverter<List<IDevice>, GetUsersDevicesQueryResponse> getUsersDevicesQueryResponseConverter)
    : IQueryHandler<GetUsersDevicesQueryResponse>
{
    public async Task<GetUsersDevicesQueryResponse> HandleAsync()
    {
        var devices = await deviceService.GetUsersDevicesAsync();

        return getUsersDevicesQueryResponseConverter.Convert(devices);
    }
}