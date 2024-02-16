using Microsoft.Extensions.Options;
using MixServer.Domain.FileExplorer.Settings;

namespace MixServer.Domain.FileExplorer.Models;

public interface IFileExplorerRootFolderNode : IFileExplorerFolderNode
{
    /// <summary>
    /// Checks if the Folder is one of the root folder children
    /// </summary>
    bool BelongsToRoot(string? absolutePath);

    /// <summary>
    /// Checks if the folder specified is a sub folder of any of the folders configured by the user
    /// </summary>
    bool BelongsToRootChild(string? absolutePath);
}

public class FileExplorerRootFolderNode(IOptions<RootFolderSettings> rootFolderSettings)
    : FileExplorerFolderNode(new FolderInfo
    {
        Name = string.Empty,
        AbsolutePath = string.Empty,
        ParentAbsolutePath = null,
        BelongsToRoot = false,
        BelongsToRootChild = false,
        Exists = true
    }, rootFolderSettings.Value.ChildrenSplit
        .Select(folder => new FileExplorerRootChildFolderNode(new DirectoryInfo(folder)))
        .Cast<IFileExplorerNode>()
        .ToList()), IFileExplorerRootFolderNode
{
    public bool BelongsToRoot(string? absolutePath)
    {
        if (string.IsNullOrWhiteSpace(absolutePath))
        {
            return false;
        }

        return Children
            .OfType<IFileExplorerFolderNode>()
            .Any(f => f.AbsolutePath == absolutePath);
    }

    /// <inheritdoc cref="IFileExplorerRootFolderNode.BelongsToRootChild"/>
    public bool BelongsToRootChild(string? absolutePath)
    {
        var isSubFolder = Children.OfType<IFileExplorerFolderNode>()
            .Any(child =>
                !string.IsNullOrWhiteSpace(absolutePath) &&
                !string.IsNullOrWhiteSpace(child.AbsolutePath) &&
                absolutePath.StartsWith(child.AbsolutePath));

        return isSubFolder;
    }
}