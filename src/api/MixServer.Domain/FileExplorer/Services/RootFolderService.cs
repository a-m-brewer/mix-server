using Microsoft.Extensions.Options;
using MixServer.Domain.FileExplorer.Converters;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Settings;

namespace MixServer.Domain.FileExplorer.Services;

public interface IRootFolderService
{
    IFileExplorerRootFolderNode RootFolder { get; }

    /// <summary>
    /// Checks if the folder specified is a sub folder of any of the folders configured by the user
    /// </summary>
    bool IsChildOfRoot(string? absolutePath);
}

public class RootFolderService(
    IFileSystemInfoConverter fileSystemInfoConverter,
    IOptions<RootFolderSettings> rootFolderSettings) : IRootFolderService
{
    public IFileExplorerRootFolderNode RootFolder { get; } = new FileExplorerRootFolderNode
    {
        Children = rootFolderSettings.Value.ChildrenSplit
            .Select(folder => fileSystemInfoConverter.ConvertToFolderNode(folder, null, true))
            .Cast<IFileExplorerNode>()
            .ToList()
    };
    
    /// <summary>
    /// Checks if the folder specified is a sub folder of any of the folders configured by the user
    /// </summary>
    public bool IsChildOfRoot(string? absolutePath)
    {
        var isSubFolder = RootFolder.Children.OfType<IFileExplorerFolderNode>()
            .Any(child => 
                !string.IsNullOrWhiteSpace(absolutePath) &&
                !string.IsNullOrWhiteSpace(child.AbsolutePath) &&
                absolutePath.StartsWith(child.AbsolutePath));

        return isSubFolder;
    }
}