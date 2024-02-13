using MixServer.Domain.Exceptions;
using MixServer.Domain.Extensions;
using MixServer.Domain.FileExplorer.Converters;
using MixServer.Domain.FileExplorer.Entities;
using MixServer.Domain.FileExplorer.Enums;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Services;
using MixServer.Domain.FileExplorer.Services.Caching;
using MixServer.Domain.Sessions.Repositories;
using MixServer.Infrastructure.Extensions;
using MixServer.Infrastructure.Users.Repository;

namespace MixServer.Infrastructure.Files.Services;

public class FileService(
    ICurrentUserRepository currentUserRepository,
    IFileSystemInfoConverter fileSystemInfoConverter,
    IFolderCacheService folderCacheService,
    IFolderSortRepository folderSortRepository,
    IMimeTypeService mimeTypeService,
    IRootFolderService rootFolderService)
    : IFileService
{
    public Task<IFileExplorerFolderNode> GetFolderAsync(string absolutePath)
    {
        return GetFolderAsync(absolutePath, true, true);
    }

    public IFileExplorerFolderNode GetUnpopulatedFolder(string absolutePath)
    {
        var isChild = rootFolderService.IsChildOfRoot(absolutePath);

        // Out of bounds of the configured folders. Therefore the user does not have permission to access it.
        if (!isChild)
        {
            return fileSystemInfoConverter.ConvertToFolderNode(absolutePath, null, false);
        }
        
        var parent = Directory.GetParent(absolutePath);

        var parentPath = rootFolderService.IsChildOfRoot(parent?.FullName)
            ? parent?.FullName
            : null;

        var folder = fileSystemInfoConverter.ConvertToFolderNode(absolutePath, parentPath, true);

        return folder;
    }

    public async Task<IFileExplorerFolderNode> GetFolderOrRootAsync(string? absolutePath)
    {
        var rootFolder = rootFolderService.RootFolder;
        
        // If no folder is specified return the root folder
        if (string.IsNullOrWhiteSpace(absolutePath))
        {
            return rootFolder;
        }

        var isChild = rootFolderService.IsChildOfRoot(absolutePath);

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

    private IFileExplorerFileNode GetFile(string fileAbsolutePath)
    {
        var parentDirectoryPath = fileAbsolutePath.GetParentFolderPathOrThrow();
        var parent = GetUnpopulatedFolder(parentDirectoryPath);
    
        return GetFile(fileAbsolutePath, parent);
    }

    public IFileExplorerFileNode GetFile(string fileAbsolutePath, IFileExplorerFolderNode parent)
    {
        var fileName = Path.GetFileName(fileAbsolutePath);
        var mimeType = mimeTypeService.GetMimeType(fileAbsolutePath);
        
        return new FileExplorerFileNode(fileName, mimeType, File.Exists(fileAbsolutePath), parent);
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
        var cacheItem = await folderCacheService.GetOrAddAsync(absolutePath);
        var isChildOrRoot = rootFolderService.IsChildOfRoot(absolutePath);
        var folder = fileSystemInfoConverter.ConvertToFolderNode(cacheItem.DirectoryInfo, isChildOrRoot);

        if (!folder.Exists)
        {
            return folder;
        }

        await currentUserRepository.LoadFileSortByAbsolutePathAsync(absolutePath);
        folder.Sort = currentUserRepository.CurrentUser.GetSortOrDefault(absolutePath);
        
        var folders = includeFolders
            ? cacheItem.Directories
                .OrderNodes(folder.Sort)
                .Select(f => fileSystemInfoConverter.ConvertToFolderNode(f, true))
            : Array.Empty<FileExplorerFolderNode>();

        IEnumerable<IFileExplorerFileNode> files = new List<IFileExplorerFileNode>();
        if (includeFiles)
        {
            // Create a duplicate folder with no children to avoid recursive loop
            var parentFolder = fileSystemInfoConverter.ConvertToFolderNode(cacheItem.DirectoryInfo, isChildOrRoot);

            files = cacheItem.Files
                .OrderNodes(folder.Sort)
                .Select(f => fileSystemInfoConverter.ConvertToFileNode(f, parentFolder));
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
}