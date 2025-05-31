using MixServer.Domain.FileExplorer.Enums;

namespace MixServer.Domain.FileExplorer.Models.Indexing;

public class RootChildChangeEvent : IEquatable<RootChildChangeEvent>
{
    public required string FullName { get; init; }
    public required RootFolderChangeType RootFolderChangeType { get; init; }
    public required WatcherChangeTypes WatcherChangeType { get; init; }
    public required string OldFullName { get; init; }

    public bool Equals(RootChildChangeEvent? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return
            FullName == other.FullName &&
            RootFolderChangeType == other.RootFolderChangeType;
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

        return Equals((RootChildChangeEvent)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(FullName, (int)RootFolderChangeType);
    }
}