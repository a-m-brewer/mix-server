using System.Collections.Concurrent;
using MixServer.Domain.FileExplorer.Converters;
using MixServer.Domain.FileExplorer.Enums;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Models.Caching;
using MixServer.Domain.FileExplorer.Services.Caching;
using MixServer.FolderIndexer.Interface.Api;
using MixServer.FolderIndexer.Interface.Models;

namespace MixServer.Domain.FileExplorer.Services.Indexing;

public class IndexFolderCacheService(IFolderIndexerFileSystemApi api,
    IFileMetadataConverter metadataConverter) : IFolderCacheService
{
    public event EventHandler<IFileExplorerNode>? ItemAdded;
    public event EventHandler<FolderCacheServiceItemUpdatedEventArgs>? ItemUpdated;
    public event EventHandler<FolderCacheServiceItemRemovedEventArgs>? ItemRemoved;
    public event EventHandler<IFileExplorerFolder>? FolderAdded;
    public event EventHandler<IFileExplorerFolder>? FolderRemoved;

    public async Task<ICacheFolder> GetOrAddAsync(string absolutePath)
    {
        var folder = await api.GetDirectoryInfoAsync(absolutePath);

        var node = ConvertFromDirectoryInfo(folder, true);
        var feFolder = new FileExplorerFolder(node);

        foreach (var child in folder.ChildItems)
        {
            IFileExplorerNode childNode = child switch
            {
                IDirectoryInfo directoryInfo => ConvertFromDirectoryInfo(directoryInfo, false),
                IFileInfo fileInfo => ConvertFromFileInfo(fileInfo, false),
                _ => throw new NotSupportedException($"Unsupported child type: {child.GetType()}")
            };
            feFolder.AddChild(childNode);
        }

        return new CacheFolderDto(feFolder);
    }

    public async Task<IFileExplorerFileNode> GetFileAsync(string fileAbsolutePath)
    {
        var file = await api.GetFileInfoAsync(fileAbsolutePath);
        
        var node = ConvertFromFileInfo(file, true);

        return node;
    }

    public async Task<(IFileExplorerFolder Parent, IFileExplorerFileNode File)> GetFileAndFolderAsync(string absoluteFilePath)
    {
        var file = await api.GetFileInfoAsync(absoluteFilePath);
        
        var node = ConvertFromFileInfo(file, true);

        var parentNode = node.Parent;
        
        var parentFolder = new FileExplorerFolder(parentNode, new ConcurrentDictionary<string, IFileExplorerNode>());
        
        return (parentFolder, node);
    }

    public void InvalidateFolder(string absolutePath)
    {
        // Do nothing, remove once folder indexing is finished
    }

    public async Task<IFileExplorerFolder> GetRootFolderAsync()
    {
        var rootDirs = await api.GetRootDirectoriesAsync();

        var root = GetRoot();

        var children = rootDirs.ToDictionary(k => k.FullName, v => ConvertFromDirectoryInfo(v, false));

        var folder = new FileExplorerFolder(root,
            new ConcurrentDictionary<string, IFileExplorerNode>(children.Select(s =>
                new KeyValuePair<string, IFileExplorerNode>(s.Key, s.Value))));
        
        return folder;
    }

    private IFileExplorerFolderNode ConvertFromDirectoryInfo(IDirectoryInfo directoryInfo, bool includeParent)
    {
        var parent = includeParent ? GetParent(directoryInfo) : null;

        var node = new FileExplorerFolderNode(
            directoryInfo.Name,
            directoryInfo.FullName,
            FileExplorerNodeType.Folder,
            directoryInfo.Exists,
            directoryInfo.CreationTimeUtc,
            directoryInfo.IsRoot,
            true,
            parent);
        
        return node;
    }

    private IFileExplorerFileNode ConvertFromFileInfo(IFileInfo fileInfo, bool includeParentsParent)
    {
        
        var parent = ConvertFromDirectoryInfo(fileInfo.ParentDirectory, includeParentsParent);
        
        var node = new FileExplorerFileNode(
            fileInfo.Name,
            fileInfo.FullName,
            fileInfo.Extension,
            FileExplorerNodeType.File,
            fileInfo.Exists,
            fileInfo.CreationTimeUtc,
            metadataConverter.Convert(fileInfo),
            parent);
        
        return node;
    }

    private IFileExplorerFolderNode? GetParent(IDirectoryInfo directoryInfo)
    {
        if (directoryInfo.ParentDirectory is not null)
        {
            return ConvertFromDirectoryInfo(directoryInfo.ParentDirectory, false);
        }

        if (directoryInfo.IsRoot)
        {
            return GetRoot();
        }

        return null;
    }

    private IFileExplorerFolderNode GetRoot()
    {
        return new FileExplorerFolderNode(string.Empty, string.Empty, FileExplorerNodeType.Folder, true,
            DateTime.MinValue, false, false, null);
    }
}