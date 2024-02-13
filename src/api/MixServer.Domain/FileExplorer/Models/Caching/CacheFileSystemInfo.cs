namespace MixServer.Domain.FileExplorer.Models.Caching;

public interface ICacheFileSystemInfo
{
    string Name { get; }
    
    string FullName { get; }
    
    DateTime CreationTimeUtc { get; }
    
    string? ParentDirectory { get; }
    
    bool Exists { get; }
}

public abstract class CacheFileSystemInfo(string name, string fullName, DateTime creationTimeUtc, string? parentDirectory, bool exists)
    : ICacheFileSystemInfo
{
    public string Name { get; } = name;
    public string FullName { get; } = fullName;
    public DateTime CreationTimeUtc { get; } = creationTimeUtc;

    public string? ParentDirectory { get; } = parentDirectory;

    public bool Exists { get; } = exists;
}