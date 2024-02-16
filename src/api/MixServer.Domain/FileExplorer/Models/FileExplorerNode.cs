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