using MixServer.Domain.Exceptions;
using MixServer.Domain.Extensions;
using MixServer.Domain.FileExplorer.Converters;
using MixServer.Domain.FileExplorer.Entities;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Services;
using MixServer.Domain.FileExplorer.Services.Caching;
using MixServer.Domain.Sessions.Repositories;
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
    public async Task<IFileExplorerFolderNode> GetFolderAsync(string absolutePath)
    {
        var cacheItem = await folderCacheService.GetOrAddAsync(absolutePath);

        var folder = cacheItem.Node;

        if (!folder.Exists)
        {
            return folder;
        }

        await currentUserRepository.LoadFileSortByAbsolutePathAsync(absolutePath);
        folder.Sort = currentUserRepository.CurrentUser.GetSortOrDefault(absolutePath);
    
        return folder;
    }

    public IFileExplorerFolderNode GetUnpopulatedFolder(string absolutePath)
    {
        var isChild = rootFolderService.IsChildOfRoot(absolutePath);

        // Out of bounds of the configured folders. Therefore the user does not have permission to access it.
        if (!isChild)
        {
            return fileSystemInfoConverter.ConvertToFolderNode(absolutePath, false);
        }
        
        var parent = Directory.GetParent(absolutePath);

        var parentPath = rootFolderService.IsChildOfRoot(parent?.FullName)
            ? parent?.FullName
            : null;

        var folder = fileSystemInfoConverter.ConvertToFolderNode(absolutePath, true);

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
            throw new ForbiddenRequestException("You do not have permission to access this folder");
        }
        
        var folder = await GetFolderAsync(absolutePath);

        return folder.Exists
            ? folder
            : throw new NotFoundException("Folder", absolutePath);
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
        var parent = fileSystemInfoConverter.ConvertToFolderNode(parentDirectoryPath, rootFolderService.IsChildOfRoot(parentDirectoryPath));
    
        return fileSystemInfoConverter.ConvertToFileNode(fileAbsolutePath, parent.Info);
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
}