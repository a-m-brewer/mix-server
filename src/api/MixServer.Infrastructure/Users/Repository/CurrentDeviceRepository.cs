using Microsoft.AspNetCore.Http;
using MixServer.Domain.Exceptions;
using MixServer.Domain.Users.Repositories;
using MixServer.Infrastructure.Users.Constants;

namespace MixServer.Infrastructure.Users.Repository;

public class CurrentDeviceRepository(IHttpContextAccessor httpContextAccessor) : ICurrentDeviceRepository
{
    public Guid DeviceId
    {
        get
        {
            var deviceIdString = httpContextAccessor.HttpContext?.User.Claims
                .SingleOrDefault(s => s.Type == CustomClaimTypes.DeviceId)?.Value;

            if (string.IsNullOrWhiteSpace(deviceIdString))
            {
                throw new UnauthorizedRequestException();
            }

            if (!Guid.TryParse(deviceIdString, out var deviceId) || deviceId == Guid.Empty)
            {
                throw new UnauthorizedRequestException();
            }

            return deviceId;
        }
    }
}