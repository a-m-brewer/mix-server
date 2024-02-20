using Microsoft.Extensions.Options;
using MixServer.Domain.FileExplorer.Enums;
using MixServer.Domain.FileExplorer.Settings;

namespace MixServer.Domain.FileExplorer.Models;

public class RootFileExplorerFolder(IOptions<RootFolderSettings> rootFolderSettings)
    : FileExplorerFolder(new FileExplorerFolderNode(string.Empty, string.Empty, FileExplorerNodeType.Folder, true,
        DateTime.MinValue, false, false, null)), IRootFileExplorerFolder
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

    public void RefreshChildren()
    {
        ChildNodes.Clear();
        
        foreach (var folder in rootFolderSettings.Value.ChildrenSplit)
        {
            var directoryInfo = new DirectoryInfo(folder);
            ChildNodes.Add(new FileExplorerFolderNode(directoryInfo.Name, directoryInfo.FullName,
                FileExplorerNodeType.Folder, directoryInfo.Exists, directoryInfo.CreationTimeUtc, true, false, Node));
        }
    }
}