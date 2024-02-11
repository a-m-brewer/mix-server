namespace MixServer.SignalR;

public struct SignalRConnectionId : IEquatable<SignalRConnectionId>
{
    private readonly string _id;
    public SignalRConnectionId(string connectionId) : this() { _id = connectionId; }

    public static bool operator ==(SignalRConnectionId left, SignalRConnectionId right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(SignalRConnectionId left, SignalRConnectionId right)
    {
        return !(left == right);
    }

    public bool Equals(SignalRConnectionId other)
    {
        return _id == other._id;
    }

    public override bool Equals(object? obj)
    {
        return obj is SignalRConnectionId connectionId && Equals(connectionId);
    }

    public override int GetHashCode()
    {
        return _id == null ? 0 : _id.GetHashCode();
    }

    /// <summary>
    /// Returns the SignalR ConnectionId
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return _id;
    }
}