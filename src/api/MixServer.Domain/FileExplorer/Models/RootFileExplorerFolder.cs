using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Options;
using MixServer.Domain.Exceptions;
using MixServer.Domain.FileExplorer.Enums;
using MixServer.Domain.FileExplorer.Settings;
using MixServer.Domain.Settings;

namespace MixServer.Domain.FileExplorer.Models;

public class RootFileExplorerFolder : FileExplorerFolder, IRootFileExplorerFolder
{
    private readonly IOptions<CacheFolderSettings> _cacheFolderSettings;
    private readonly IOptions<RootFolderSettings> _rootFolderSettings;

    public RootFileExplorerFolder(
        IOptions<CacheFolderSettings> cacheFolderSettings,
        IOptions<RootFolderSettings> rootFolderSettings) 
        : base(
            new FileExplorerFolderNode(
                new NodePath(string.Empty, string.Empty),
                FileExplorerNodeType.Folder, 
                true, DateTime.MinValue, 
                false, 
                false, 
                null))
    {
        _cacheFolderSettings = cacheFolderSettings;
        _rootFolderSettings = rootFolderSettings;
        RefreshChildren();
    }

    public NodePath GetNodePath(string absolutePath)
    {
        if (string.IsNullOrWhiteSpace(absolutePath))
        {
            return Node.Path;
        }
        
        if (TryGetNodePath(absolutePath, out var absoluteNodePath))
        {
            return absoluteNodePath;
        }
        
        if (TryGetCacheFolderNodePath(absolutePath, out var cacheNodePath))
        {
            return cacheNodePath;
        }
        
        throw new NotFoundException(nameof(IRootFileExplorerFolder), absolutePath);
    }

    public IFileExplorerFolderNode GetRootChildOrThrow(NodePath nodePath)
    {
        return Children
                   .OfType<IFileExplorerFolderNode>()
                   .FirstOrDefault(f => f.Path.RootPath == nodePath.RootPath)
            ?? throw new NotFoundException(nameof(Children), nodePath.RootPath);
    }

    private bool TryGetNodePath(string? absolutePath, [MaybeNullWhen(false)] out NodePath nodePath)
    {
        if (string.IsNullOrWhiteSpace(absolutePath))
        {
            nodePath = null;
            return false;
        }

        var child = Children.FirstOrDefault(f => absolutePath.StartsWith(f.Path.AbsolutePath));

        if (child is null)
        {
            nodePath = null;
            return false;
        }

        nodePath = absolutePath == child.Path.RootPath
            ? child.Path
            : child.Path with { RelativePath = Path.GetRelativePath(child.Path.RootPath, absolutePath) };
        return true;
    }
    
    private bool TryGetCacheFolderNodePath(string? absolutePath, [MaybeNullWhen(false)] out NodePath nodePath)
    {
        if (string.IsNullOrWhiteSpace(absolutePath))
        {
            nodePath = null;
            return false;
        }

        var cacheFolderAbsolutePath = _cacheFolderSettings.Value.DirectoryAbsolutePath;
        if (absolutePath.StartsWith(cacheFolderAbsolutePath))
        {
            var relativePath = absolutePath == cacheFolderAbsolutePath
                ? string.Empty
                : Path.GetRelativePath(cacheFolderAbsolutePath, absolutePath);
            
            nodePath = new NodePath(cacheFolderAbsolutePath, relativePath);
            return true;
        }
        
        nodePath = null;
        return false;
    }
    
    public bool DescendantOfRoot(NodePath nodePath)
    {
        return !nodePath.IsRoot && 
               Children.Any(f => f.Path.RootPath == nodePath.RootPath);
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
}