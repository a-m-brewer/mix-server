using System.Text.RegularExpressions;
using MixServer.Domain.FileExplorer.Enums;

namespace MixServer.Domain.FileExplorer.Models;

public partial class FileExplorerFileNode(
    string name,
    string absolutePath,
    FileExplorerNodeType type,
    bool exists,
    DateTime creationTimeUtc,
    string mimeType,
    IFileExplorerFolderNodeInfo parent)
    : IFileExplorerFileNode
{
    public string Name { get; } = name;
    public string AbsolutePath { get; } = absolutePath;
    public FileExplorerNodeType Type { get; } = type;
    public bool Exists { get; } = exists;
    public DateTime CreationTimeUtc { get; } = creationTimeUtc;
    public string MimeType { get; } = mimeType;
    public bool PlaybackSupported => (Parent?.BelongsToRootChild ?? false) &&
                                     Exists && 
                                     !string.IsNullOrWhiteSpace(MimeType) &&
                                     AudioVideoMimeTypeRegex().IsMatch(MimeType);
    public IFileExplorerFolderNodeInfo Parent { get; } = parent;
    
    [GeneratedRegex(@"^(audio|video)\/(.*)")]
    private static partial Regex AudioVideoMimeTypeRegex();
}