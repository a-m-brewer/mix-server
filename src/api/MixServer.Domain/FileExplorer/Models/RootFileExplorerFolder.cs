using Microsoft.Extensions.Options;
using MixServer.Domain.Exceptions;
using MixServer.Domain.FileExplorer.Enums;
using MixServer.Domain.FileExplorer.Settings;

namespace MixServer.Domain.FileExplorer.Models;

public class RootFileExplorerFolder : FileExplorerFolder, IRootFileExplorerFolder
{
    private readonly IOptions<RootFolderSettings> _rootFolderSettings;

    public RootFileExplorerFolder(IOptions<RootFolderSettings> rootFolderSettings) 
        : base(
            new FileExplorerFolderNode(
                new NodePath(string.Empty, string.Empty),
                FileExplorerNodeType.Folder, 
                true, DateTime.MinValue, 
                false, 
                false, 
                null))
    {
        _rootFolderSettings = rootFolderSettings;
        RefreshChildren();
    }
    
    public bool BelongsToRoot(string? rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            return false;
        }

        return Children
            .OfType<IFileExplorerFolderNode>()
            .Any(f => f.Path.RootPath == rootPath);
    }

    public bool BelongsToRootChild(string rootPath)
    {
        var isSubFolder = Children.OfType<IFileExplorerFolderNode>()
            .Any(child =>
                !string.IsNullOrWhiteSpace(rootPath) &&
                !string.IsNullOrWhiteSpace(child.Path.RootPath) &&
                rootPath.StartsWith(child.Path.RootPath));

        return isSubFolder;
    }

    public void RefreshChildren()
    {
        ChildNodes.Clear();
        
        foreach (var folder in _rootFolderSettings.Value.ChildrenSplit)
        {
            var directoryInfo = new DirectoryInfo(folder);
            ChildNodes[directoryInfo.Name] = new FileExplorerFolderNode(new NodePath(directoryInfo.FullName, string.Empty),
                FileExplorerNodeType.Folder, directoryInfo.Exists, directoryInfo.CreationTimeUtc, true, false, Node);
        }
    }

    public NodePath GetNodePath(string absolutePath)
    {
        var root = GetRoot(absolutePath);
        var relativePath = Path.GetRelativePath(root.Path.AbsolutePath, absolutePath);
        
        return new NodePath(root.Path.AbsolutePath, relativePath);
    }

    private IFileExplorerFolderNode GetRoot(string absolutePath)
    {
        if (BelongsToRoot(absolutePath))
        {
            return Node;
        }
        
        var rootChild = Children.OfType<IFileExplorerFolderNode>()
            .FirstOrDefault(child =>
                !string.IsNullOrWhiteSpace(absolutePath) &&
                !string.IsNullOrWhiteSpace(child.Path.RootPath) &&
                absolutePath.StartsWith(child.Path.RootPath));
        
        return rootChild ?? throw new NotFoundException(nameof(IRootFileExplorerFolder), absolutePath);
    }
}