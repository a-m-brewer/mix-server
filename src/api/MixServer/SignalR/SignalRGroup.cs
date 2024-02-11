namespace MixServer.SignalR;

public struct SignalRGroup : IEquatable<SignalRGroup>
{
    public SignalRGroup(string groupName) : this() { Name = groupName; }
    public string Name { get; }

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