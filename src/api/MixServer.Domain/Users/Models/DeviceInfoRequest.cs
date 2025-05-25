using Microsoft.Extensions.Primitives;

namespace MixServer.Domain.Users.Models;

public record DeviceInfoRequest(Guid DeviceId, Dictionary<string, StringValues> Headers);
