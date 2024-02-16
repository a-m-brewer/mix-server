using MixServer.Domain.FileExplorer.Enums;

namespace MixServer.Domain.FileExplorer.Models;

public class RootFileExplorerFolder() : FileExplorerFolder(
    new FileExplorerFolderNode(string.Empty, string.Empty, FileExplorerNodeType.Folder, true, DateTime.MinValue, false, false)), IRootFileExplorerFolder
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