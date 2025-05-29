using Microsoft.Extensions.Primitives;

namespace MixServer.Domain.Users.Models;

public class DeviceInfoRequest : IEquatable<DeviceInfoRequest>
{
    public required Guid DeviceId { get; init; } 
    public required Dictionary<string, StringValues> Headers { get; init; }

    public bool Equals(DeviceInfoRequest? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return DeviceId.Equals(other.DeviceId);
    }

    public override bool Equals(object? obj)
    {
        if (obj is null)
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj.GetType() != GetType())
        {
            return false;
        }

        return Equals((DeviceInfoRequest)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(DeviceId);
    }
}
