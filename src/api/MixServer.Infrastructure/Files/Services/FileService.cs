using Microsoft.Extensions.Options;
using MixServer.Domain.Exceptions;
using MixServer.Domain.Extensions;
using MixServer.Domain.FileExplorer.Entities;
using MixServer.Domain.FileExplorer.Enums;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Services;
using MixServer.Domain.FileExplorer.Settings;
using MixServer.Domain.Sessions.Repositories;
using MixServer.Infrastructure.Extensions;
using MixServer.Infrastructure.Users.Repository;

namespace MixServer.Infrastructure.Files.Services;

public class FileService(
    ICurrentUserRepository currentUserRepository,
    IFolderSortRepository folderSortRepository,
    IMimeTypeService mimeTypeService,
    IOptions<RootFolderSettings> rootFolderSettings)
    : IFileService
{
    public IFileExplorerRootFolderNode GetRootFolder()
    {
        return new FileExplorerRootFolderNode
        {
            Children = rootFolderSettings.Value.ChildrenSplit
                .Select(folder => new FileExplorerFolderNode(folder, null, true))
                .Cast<IFileExplorerNode>()
                .ToList()
        };
    }

    public Task<IFileExplorerFolderNode> GetFolderAsync(string absolutePath)
    {
        return GetFolderAsync(absolutePath, true, true);
    }

    public IFileExplorerFolderNode GetUnpopulatedFolder(string absolutePath)
    {
        var isChild = IsChildOfRoot(absolutePath);

        // Out of bounds of the configured folders. Therefore the user does not have permission to access it.
        if (!isChild)
        {
            return new FileExplorerFolderNode(absolutePath, null, false);
        }
        
        var parent = Directory.GetParent(absolutePath);

        var parentPath = IsChildOfRoot(parent?.FullName)
            ? parent?.FullName
            : null;

        var folder = new FileExplorerFolderNode(absolutePath, parentPath, true);

        return folder;
    }

    public async Task<IFileExplorerFolderNode> GetFolderOrRootAsync(string? absolutePath)
    {
        var rootFolder = GetRootFolder();
        
        // If no folder is specified return the root folder
        if (string.IsNullOrWhiteSpace(absolutePath))
        {
            return rootFolder;
        }

        var isChild = IsChildOfRoot(absolutePath);

        // The folder is out of bounds return the root folder instead
        if (!isChild)
        {
            return rootFolder;
        }
        
        var folder = await GetFolderAsync(absolutePath);

        return folder.Exists
            ? folder
            : rootFolder;
    }

    public Task<IFileExplorerFolderNode> GetFilesInFolderAsync(string absolutePath)
    {
        return GetFolderAsync(absolutePath, true, false);
    }

    public List<IFileExplorerFileNode> GetFiles(IReadOnlyList<string> absoluteFilePaths)
    {
        return absoluteFilePaths.Select(GetFile).ToList();
    }
    
    public IFileExplorerFileNode GetFile(string absoluteFolderPath, string filename)
    {
        return GetFile(Path.Join(absoluteFolderPath, filename));
    }
    
    public IFileExplorerFileNode GetFile(string fileAbsolutePath)
    {
        var parentDirectoryPath = fileAbsolutePath.GetParentFolderPathOrThrow();
        var parent = GetUnpopulatedFolder(parentDirectoryPath);

        return GetFile(fileAbsolutePath, parent);
    }

    public IFileExplorerFileNode GetFile(string fileAbsolutePath, IFileExplorerFolderNode parent)
    {
        var fileName = Path.GetFileName(fileAbsolutePath);
        var mimeType = mimeTypeService.GetMimeType(fileAbsolutePath);
        
        return new FileExplorerFileNode(fileName, mimeType, parent);
    }

    public async Task SetFolderSortAsync(IFolderSortRequest request)
    {
        await currentUserRepository.LoadFileSortByAbsolutePathAsync(request.AbsoluteFolderPath);
        var user = currentUserRepository.CurrentUser;

        var sort = user.FolderSorts.SingleOrDefault(s => s.AbsoluteFolderPath == request.AbsoluteFolderPath);

        if (sort == null)
        {
            var folderSort = new FolderSort(
                Guid.NewGuid(),
                request.AbsoluteFolderPath,
                request.Descending,
                request.SortMode)
            {
                UserId = user.UserName ?? throw new UnauthorizedRequestException()
            };

            await folderSortRepository.AddAsync(folderSort);
            user.FolderSorts.Add(folderSort);
        }
        else
        {
            sort.Update(request);
        }
    }
    
    private async Task<IFileExplorerFolderNode> GetFolderAsync(string absolutePath, bool includeFiles, bool includeFolders)
    {
        var folder = GetUnpopulatedFolder(absolutePath);

        if (!folder.Exists)
        {
            return folder;
        }

        await currentUserRepository.LoadFileSortByAbsolutePathAsync(absolutePath);
        folder.Sort = currentUserRepository.CurrentUser.GetSortOrDefault(absolutePath);
        
        var directoryInfo = new DirectoryInfo(absolutePath);
        
        var folders = includeFolders
            ? directoryInfo.GetDirectories()
                .OrderNodes(folder.Sort)
                .Select(f => new FileExplorerFolderNode(f.FullName, folder.AbsolutePath, true))
            : Array.Empty<FileExplorerFolderNode>();

        IEnumerable<IFileExplorerFileNode> files = new List<IFileExplorerFileNode>();
        if (includeFiles)
        {
            // Create a duplicate folder with no children to avoid recursive loop
            var parentFolder = new FileExplorerFolderNode(folder.AbsolutePath, folder.ParentAbsolutePath, folder.CanRead);

            files = directoryInfo.GetFiles()
                .OrderNodes(folder.Sort)
                .Select(file => GetFile(file.FullName, parentFolder));
        }

        switch (folder.Sort.SortMode)
        {
            case FolderSortMode.Created:
                folder.Children.AddRange(files);
                folder.Children.AddRange(folders);
                break;
            case FolderSortMode.Name:
            default:
                folder.Children.AddRange(folders);
                folder.Children.AddRange(files);
                break;
        }

        return folder;
    }

    /// <summary>
    /// Checks if the folder specified is a sub folder of any of the folders configured by the user
    /// </summary>
    private bool IsChildOfRoot(string? absolutePath)
    {
        var rootFolder = GetRootFolder();
        
        var isSubFolder = rootFolder.Children.OfType<IFileExplorerFolderNode>()
            .Any(child => 
                !string.IsNullOrWhiteSpace(absolutePath) &&
                !string.IsNullOrWhiteSpace(child.AbsolutePath) &&
                absolutePath.StartsWith(child.AbsolutePath));

        return isSubFolder;
    }
}