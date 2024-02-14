using MixServer.Domain.Extensions;
using MixServer.Domain.FileExplorer.Enums;

namespace MixServer.Domain.FileExplorer.Models;

public interface IFileExplorerNode
{
    string Name { get; }
    /// <summary>
    /// The files name with characters replaced to make it a valid HTML id.
    /// This is to allow linking to a specific file.
    /// </summary>
    string NameIdentifier { get; }
    FileExplorerNodeType Type { get; }
    bool Exists { get; }
    string? AbsolutePath { get; }
    DateTime CreationTimeUtc { get; }
}

public abstract class FileExplorerNode(FileExplorerNodeType type) : IFileExplorerNode
{
    public abstract string Name { get; }
    
    public string NameIdentifier => Name
        .ToValidHtmlId() ?? string.Empty;

    public FileExplorerNodeType Type { get; } = type;

    public abstract bool Exists { get; }

    public abstract string? AbsolutePath { get; }
    public abstract DateTime CreationTimeUtc { get; }
}