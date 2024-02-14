namespace MixServer.SignalR;

public struct SignalRGroup(string groupName) : IEquatable<SignalRGroup>
{
    public string Name { get; } = groupName;

    public static implicit operator string(SignalRGroup g) => g.Name;

    public bool Equals(SignalRGroup other)
    {
        return Name == other.Name;
    }

    public override bool Equals(object? obj)
    {
        return obj is SignalRGroup group && Equals(group);
    }

    public override int GetHashCode()
    {
        return Name == null ? 0 : Name.GetHashCode();
    }

    public override string ToString()
    {
        return Name;
    }
}