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
    IFolderPersistenceService folderPersistenceService,
    IFolderSortRepository folderSortRepository,
    IRootFileExplorerFolder rootFolder)
    : IFileService
{
    public async Task<IFileExplorerFolder> GetFolderAsync(NodePath nodePath, CancellationToken cancellationToken)
    {
        var cacheItem = await folderCacheService.GetOrAddAsync(nodePath);

        var folder = cacheItem.Folder;

        if (!folder.Node.Exists)
        {
            return folder;
        }

        await currentUserRepository.LoadFileSortByAbsolutePathAsync(nodePath, cancellationToken);
        folder.Sort = (await currentUserRepository.GetCurrentUserAsync()).GetSortOrDefault(nodePath);
    
        return folder;
    }

    public async Task<IFileExplorerFolder> GetFolderOrRootAsync(NodePath? nodePath, CancellationToken cancellationToken)
    {
        // If no folder is specified return the root folder
        if (nodePath is null || nodePath.IsRoot)
        {
            return rootFolder;
        }

        // The folder is out of bounds return the root folder instead
        if (!rootFolder.DescendantOfRoot(nodePath))
        {
            throw new ForbiddenRequestException("You do not have permission to access this folder");
        }
        
        var folder = await GetFolderAsync(nodePath, cancellationToken);

        return folder.Node.Exists
            ? folder
            : throw new NotFoundException("Folder", nodePath.AbsolutePath);
    }

    public async Task<List<IFileExplorerFileNode>> GetFilesAsync(IReadOnlyList<NodePath> absoluteFilePaths)
    {
        var groupedPaths = await Task.WhenAll(absoluteFilePaths
            .Select(s => s.Parent)
            .DistinctBy(d => d.AbsolutePath)
            .Select(folderCacheService.GetOrAddAsync));

        var files = new List<IFileExplorerFileNode>();
        foreach (var nodePath in absoluteFilePaths)
        {
            var parent = groupedPaths.First(f => f.Folder.Node.Path.IsEqualTo(nodePath.Parent)).Folder;
            var file = parent.Children
                .OfType<IFileExplorerFileNode>()
                .FirstOrDefault(f => f.Path.IsEqualTo(nodePath)) ?? 
                       fileExplorerConverter.Convert(new FileInfo(nodePath.AbsolutePath), parent.Node);

            files.Add(file);
        }
        
        return files;
    }

    public async Task<IFileExplorerFileNode> GetFileAsync(NodePath nodePath)
    {
        return await folderCacheService.GetFileAsync(nodePath);
    }

    public void CopyNode(
        NodePath sourcePath,
        NodePath destinationPath,
        bool move,
        bool overwrite)
    {
        var destinationFolderType = GetNodeTypeOrThrow(destinationPath.Parent);
        switch (destinationFolderType)
        {
            case FileExplorerNodeType.File:
                throw new InvalidRequestException(nameof(destinationPath.Parent), $"{destinationPath.Parent.AbsolutePath} is a file");
            case FileExplorerNodeType.Folder:
                break;
            default:
                throw new NotFoundException(nameof(destinationPath.Parent), destinationPath.Parent.AbsolutePath);
        }
        
        if (File.Exists(destinationPath.AbsolutePath) && !overwrite)
        {
            throw new ConflictException(destinationPath.Parent.AbsolutePath, destinationPath.FileName);
        }
        
        var type = GetNodeTypeOrThrow(sourcePath);

        if (type == FileExplorerNodeType.File)
        {
            if (move)
            {
                File.Move(sourcePath.AbsolutePath, destinationPath.AbsolutePath);
            }
            else
            {
                File.Copy(sourcePath.AbsolutePath, destinationPath.AbsolutePath, overwrite);
            }
        }
        else
        {
            throw new NotSupportedException("Copying folders is not supported");
        }
    }

    public void DeleteNode(NodePath nodePath)
    {
        // check if file or folder
        var type = GetNodeTypeOrThrow(nodePath);
        
        if (type == FileExplorerNodeType.File)
        {
            File.Delete(nodePath.AbsolutePath);
        }
        else
        {
            throw new NotSupportedException("Deleting folders is not supported");
            // Directory.Delete(absolutePath, true);
        }
    }

    private static FileExplorerNodeType GetNodeTypeOrThrow(NodePath nodePath) =>
        GetNodeType(nodePath) ?? throw new NotFoundException("Node", nodePath.AbsolutePath);

    private static FileExplorerNodeType? GetNodeType(NodePath nodePath)
    {
        if (File.Exists(nodePath.AbsolutePath))
        {
            return FileExplorerNodeType.File;
        }
        
        if (Directory.Exists(nodePath.AbsolutePath))
        {
            return FileExplorerNodeType.Folder;
        }
        
        return null;
    }

    public async Task SetFolderSortAsync(IFolderSortRequest request, CancellationToken cancellationToken)
    {
        await currentUserRepository.LoadFileSortByAbsolutePathAsync(request.Path, cancellationToken);
        var user = await currentUserRepository.GetCurrentUserAsync();

        var sort = user.FolderSorts.SingleOrDefault(s =>
            s.NodeEntity.RootChild.RelativePath == request.Path.RootPath &&
            s.NodeEntity.RelativePath == request.Path.RelativePath);

        if (sort is null)
        {
            var folder = await folderPersistenceService.GetFolderAsync(request.Path, cancellationToken);
            var folderSort = new FolderSort
            {
                Id = Guid.NewGuid(),
                NodeEntity = folder.Entity,
                NodeIdEntity = folder.Entity.Id,
                Descending = request.Descending,
                SortMode = request.SortMode,
                UserId = user.UserName ?? throw new UnauthorizedRequestException()
            };

            await folderSortRepository.AddAsync(folderSort, cancellationToken);
            user.FolderSorts.Add(folderSort);
        }
        else
        {
            sort.Update(request);
        }
    }
}