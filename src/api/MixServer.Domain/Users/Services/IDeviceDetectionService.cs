using Microsoft.Extensions.Primitives;
using MixServer.Domain.Users.Models;

namespace MixServer.Domain.Users.Services;

public interface IDeviceDetectionService
{
    IDeviceInfo GetCurrentUsersDevice(IDictionary<string, StringValues> headers);
}