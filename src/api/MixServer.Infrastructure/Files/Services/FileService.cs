using MixServer.Domain.Exceptions;
using MixServer.Domain.FileExplorer.Converters;
using MixServer.Domain.FileExplorer.Entities;
using MixServer.Domain.FileExplorer.Enums;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Services;
using MixServer.Domain.FileExplorer.Services.Caching;
using MixServer.Domain.Sessions.Repositories;
using MixServer.Infrastructure.Users.Repository;

namespace MixServer.Infrastructure.Files.Services;

public class FileService(
    ICurrentUserRepository currentUserRepository,
    IFileExplorerConverter fileExplorerConverter,
    IFolderCacheService folderCacheService,
    IFolderSortRepository folderSortRepository,
    IRootFileExplorerFolder rootFolder)
    : IFileService
{
    public async Task<IFileExplorerFolder> GetFolderAsync(string absolutePath)
    {
        var cacheItem = await folderCacheService.GetOrAddAsync(absolutePath);

        var folder = cacheItem.Folder;

        if (!folder.Node.Exists)
        {
            return folder;
        }

        await currentUserRepository.LoadFileSortByAbsolutePathAsync(absolutePath);
        folder.Sort = currentUserRepository.CurrentUser.GetSortOrDefault(absolutePath);
    
        return folder;
    }

    public async Task<IFileExplorerFolder> GetFolderOrRootAsync(string? absolutePath)
    {
        // If no folder is specified return the root folder
        if (string.IsNullOrWhiteSpace(absolutePath))
        {
            return rootFolder;
        }

        // The folder is out of bounds return the root folder instead
        if (!rootFolder.BelongsToRootChild(absolutePath))
        {
            throw new ForbiddenRequestException("You do not have permission to access this folder");
        }
        
        var folder = await GetFolderAsync(absolutePath);

        return folder.Node.Exists
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
        return fileExplorerConverter.Convert(fileAbsolutePath);
    }

    public void CopyNode(
        string sourcePath,
        string destinationFolder,
        string destinationName,
        bool move,
        bool overwrite)
    {
        var destinationFolderType = GetNodeTypeOrThrow(destinationFolder);
        switch (destinationFolderType)
        {
            case FileExplorerNodeType.File:
                throw new InvalidRequestException(nameof(destinationFolder), $"{destinationFolder} is a file");
            case FileExplorerNodeType.Folder:
                break;
            default:
                throw new NotFoundException(nameof(destinationFolder), destinationFolder);
        }
        
        var destinationPath = Path.Join(destinationFolder, destinationName);
        
        var type = GetNodeTypeOrThrow(sourcePath);

        if (type == FileExplorerNodeType.File)
        {
            if (move)
            {
                File.Move(sourcePath, destinationPath);
            }
            else
            {
                File.Copy(sourcePath, destinationPath, overwrite);
            }
        }
        else
        {
            throw new NotSupportedException("Copying folders is not supported");
        }
    }

    public void DeleteNode(string absolutePath)
    {
        // check if file or folder
        var type = GetNodeTypeOrThrow(absolutePath);
        
        if (type == FileExplorerNodeType.File)
        {
            File.Delete(absolutePath);
        }
        else
        {
            throw new NotSupportedException("Deleting folders is not supported");
            // Directory.Delete(absolutePath, true);
        }
    }

    private static FileExplorerNodeType GetNodeTypeOrThrow(string absolutePath) =>
        GetNodeType(absolutePath) ?? throw new NotFoundException("Node", absolutePath);

    private static FileExplorerNodeType? GetNodeType(string absolutePath)
    {
        if (File.Exists(absolutePath))
        {
            return FileExplorerNodeType.File;
        }
        
        if (Directory.Exists(absolutePath))
        {
            return FileExplorerNodeType.Folder;
        }
        
        return null;
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