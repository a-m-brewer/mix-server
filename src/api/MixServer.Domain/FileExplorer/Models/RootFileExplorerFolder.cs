using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using MixServer.Domain.FileExplorer.Enums;

namespace MixServer.Domain.FileExplorer.Models;

public class RootFileExplorerFolder : FileExplorerFolder, IRootFileExplorerFolder
{
    // private readonly IOptions<RootFolderSettings> _rootFolderSettings;

    public RootFileExplorerFolder() : base(new FileExplorerFolderNode(string.Empty, string.Empty, FileExplorerNodeType.Folder, true, DateTime.MinValue, false, false, null), new ConcurrentDictionary<string, IFileExplorerNode>())
    {
        // _rootFolderSettings = rootFolderSettings;
        RefreshChildren();
    }
    
    public bool BelongsToRoot(string? absolutePath)
    {
        // if (string.IsNullOrWhiteSpace(absolutePath))
        // {
        //     return false;
        // }
        //
        // return Children
        //     .OfType<IFileExplorerFolderNode>()
        //     .Any(f => f.AbsolutePath == absolutePath);

        return false;
    }

    public bool BelongsToRootChild(string? absolutePath)
    {
        // var isSubFolder = Children.OfType<IFileExplorerFolderNode>()
        //     .Any(child =>
        //         !string.IsNullOrWhiteSpace(absolutePath) &&
        //         !string.IsNullOrWhiteSpace(child.AbsolutePath) &&
        //         absolutePath.StartsWith(child.AbsolutePath));
        //
        // return isSubFolder;
        
        return false;
    }

    public void RefreshChildren()
    {
        // ChildNodes.Clear();
        //
        // foreach (var folder in _rootFolderSettings.Value.ChildrenSplit)
        // {
        //     var directoryInfo = new DirectoryInfo(folder);
        //     ChildNodes[directoryInfo.Name] = new FileExplorerFolderNode(directoryInfo.Name, directoryInfo.FullName,
        //         FileExplorerNodeType.Folder, directoryInfo.Exists, directoryInfo.CreationTimeUtc, true, false, Node);
        // }
    }
}