namespace MixServer.Domain.FileExplorer.Models;

public interface IFolderInfo
{
    string Name { get; }

    string? AbsolutePath { get; }

    string? ParentAbsolutePath { get; }

    bool Exists { get; }

    bool CanRead { get; }
    
    DateTime CreationTimeUtc { get; }
}

public class FolderInfo : IFolderInfo
{
    public string Name { get; init; } = string.Empty;
    public string? AbsolutePath { get; init; }

    public string? ParentAbsolutePath { get; init; }

    public bool Exists { get; init; }

    public bool CanRead { get; init; }

    public DateTime CreationTimeUtc { get; init; }
}