namespace MixServer.Domain.FileExplorer.Models;

public class FolderHeader : IEquatable<FolderHeader>
{
    public required NodePath NodePath { get; init; }
    
    public required string Hash { get; init; }

    public bool Equals(FolderHeader? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return NodePath.IsEqualTo(other.NodePath) &&
               Hash == other.Hash;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((FolderHeader)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(NodePath, Hash);
    }
}