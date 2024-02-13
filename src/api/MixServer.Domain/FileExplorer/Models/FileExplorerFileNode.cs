using System.Text.RegularExpressions;
using MixServer.Domain.FileExplorer.Enums;

namespace MixServer.Domain.FileExplorer.Models;

public interface IFileExplorerFileNode : IFileExplorerNode
{
    string? MimeType { get; }
    bool PlaybackSupported { get; }
    public IFileExplorerFolderNode Parent { get; }
}

public partial class FileExplorerFileNode(string name, string? mimeType, bool exists, IFileExplorerFolderNode parent)
    : FileExplorerNode(FileExplorerNodeType.File), IFileExplorerFileNode
{
    public override string Name { get; } = name;
    public override bool Exists { get; } = exists;

    public override string AbsolutePath => string.IsNullOrWhiteSpace(Parent.AbsolutePath)
        ? Name
        : Path.Join(Parent.AbsolutePath, Name);

    public string? MimeType { get; } = mimeType;

    public bool PlaybackSupported => Parent.CanRead &&
                                     Exists && 
                                     !string.IsNullOrWhiteSpace(MimeType) &&
                                     AudioVideoMimeTypeRegex().IsMatch(MimeType);

    public IFileExplorerFolderNode Parent { get; } = parent;

    [GeneratedRegex(@"^(audio|video)\/(.*)")]
    private static partial Regex AudioVideoMimeTypeRegex();
}