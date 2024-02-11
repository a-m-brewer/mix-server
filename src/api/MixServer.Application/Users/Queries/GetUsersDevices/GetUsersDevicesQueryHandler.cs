using MixServer.Domain.Interfaces;
using MixServer.Domain.Users.Entities;
using MixServer.Domain.Users.Services;

namespace MixServer.Application.Users.Queries.GetUsersDevices;

public class GetUsersDevicesQueryHandler : IQueryHandler<GetUsersDevicesQueryResponse>
{
    private readonly IDeviceService _deviceService;
    private readonly IConverter<List<IDevice>, GetUsersDevicesQueryResponse> _getUsersDevicesQueryResponseConverter;

    public GetUsersDevicesQueryHandler(
        IDeviceService deviceService,
        IConverter<List<IDevice>, GetUsersDevicesQueryResponse> getUsersDevicesQueryResponseConverter)
    {
        _deviceService = deviceService;
        _getUsersDevicesQueryResponseConverter = getUsersDevicesQueryResponseConverter;
    }
    
    public async Task<GetUsersDevicesQueryResponse> HandleAsync()
    {
        var devices = await _deviceService.GetUsersDevicesAsync();

        return _getUsersDevicesQueryResponseConverter.Convert(devices);
    }
}