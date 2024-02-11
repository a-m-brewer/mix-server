namespace MixServer.SignalR;

public struct SignalRUserId : IEquatable<SignalRUserId>
{
    public SignalRUserId(string userId) : this() { Id = userId; }
    public string Id { get; }

    public static implicit operator string(SignalRUserId c) => c.Id;

    public bool Equals(SignalRUserId other)
    {
        return Id == other.Id;
    }

    public override bool Equals(object? obj)
    {
        return obj is SignalRUserId userId && Equals(userId);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public override string ToString()
    {
        return Id;
    }
}