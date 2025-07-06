using MixServer.Domain.Exceptions;
using MixServer.Domain.FileExplorer.Converters;
using MixServer.Domain.FileExplorer.Entities;
using MixServer.Domain.FileExplorer.Enums;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Repositories;
using MixServer.Domain.FileExplorer.Repositories.DbQueryOptions;
using MixServer.Domain.FileExplorer.Services;
using MixServer.Domain.FileExplorer.Services.Caching;
using MixServer.Domain.Sessions.Repositories;
using MixServer.Infrastructure.Users.Repository;

namespace MixServer.Infrastructure.Files.Services;

public class FileService(
    IFileExplorerEntityConverter fileExplorerEntityConverter,
    IFolderPersistenceService folderPersistenceService,
    ICurrentUserRepository currentUserRepository,
    IFolderSortRepository folderSortRepository,
    IRootFileExplorerFolder rootFolder)
    : IFileService
{
    public async Task<IFileExplorerFolder> GetFolderAsync(NodePath nodePath, CancellationToken cancellationToken)
    {
        var folderEntity = await folderPersistenceService.GetOrAddFolderAsync(nodePath, cancellationToken);
        var folder = fileExplorerEntityConverter.Convert(folderEntity);

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

    public async Task<List<IFileExplorerFileNode>> GetFilesAsync(IReadOnlyList<NodePath> nodePaths)
    {
        var files = await folderPersistenceService.GetOrAddFileRangeAsync(nodePaths);
        
        return files
            .Select(fileExplorerEntityConverter.Convert)
            .ToList();
    }

    public async Task<IFileExplorerFileNode> GetFileAsync(NodePath nodePath)
    {
        var entity = await folderPersistenceService.GetOrAddFileAsync(nodePath);
        return fileExplorerEntityConverter.Convert(entity);
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

    public async Task<IFileExplorerFolder> SetFolderSortAsync(IFolderSortRequest request, CancellationToken cancellationToken)
    {
        await currentUserRepository.LoadFileSortByAbsolutePathAsync(request.Path, cancellationToken);
        var user = await currentUserRepository.GetCurrentUserAsync();

        var sort = user.FolderSorts.SingleOrDefault(s =>
            s.NodeEntity.RootChild.RelativePath == request.Path.RootPath &&
            s.NodeEntity.RelativePath == request.Path.RelativePath);
        
        var folderEntity = await folderPersistenceService.GetOrAddFolderAsync(request.Path, cancellationToken);

        if (sort is null)
        {
            if (folderEntity is not FileExplorerFolderNodeEntity folder)
            {
                throw new InvalidRequestException(request.Path.AbsolutePath, "The specified path does not point to a sortable folder");
            }

            sort = new FolderSort
            {
                Id = Guid.NewGuid(),
                NodeEntity = folder,
                NodeIdEntity = folder.Id,
                Descending = request.Descending,
                SortMode = request.SortMode,
                UserId = user.UserName ?? throw new UnauthorizedRequestException()
            };

            await folderSortRepository.AddAsync(sort, cancellationToken);
            user.FolderSorts.Add(sort);
        }
        else
        {
            sort.Update(request);
        }
        
        var folderNode = fileExplorerEntityConverter.Convert(folderEntity);
        folderNode.Sort = sort;

        return folderNode;
    }
}