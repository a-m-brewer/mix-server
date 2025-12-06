using MixServer.Domain.Exceptions;
using MixServer.Domain.FileExplorer.Converters;
using MixServer.Domain.FileExplorer.Entities;
using MixServer.Domain.FileExplorer.Enums;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Repositories;
using MixServer.Domain.FileExplorer.Repositories.DbQueryOptions;
using MixServer.Domain.FileExplorer.Services;
using MixServer.Infrastructure.EF;
using MixServer.Infrastructure.Users.Repository;
using Range = MixServer.Domain.FileExplorer.Models.Range;

namespace MixServer.Infrastructure.Files.Services;

public class FileService(
    IFileExplorerEntityConverter fileExplorerEntityConverter,
    IFolderPersistenceService folderPersistenceService,
    IFileSystemQueryService fileSystemQueryService,
    ICurrentDbUserRepository currentUserRepository,
    IFolderSortRepository folderSortRepository,
    IRootFileExplorerFolder rootFolder)
    : IFileService
{
    public async Task<IFileExplorerFolderRange> GetFolderRangeAsync(NodePath nodePath, Range range, CancellationToken cancellationToken = default)
    {
        var sort = await GetCurrentUsersFolderSortAsync(nodePath, cancellationToken);

        var folderEntity = await folderPersistenceService.GetOrAddFolderAsync(nodePath, range, sort, cancellationToken: cancellationToken);
        
        var folder = fileExplorerEntityConverter.ConvertPage(folderEntity, sort);

        return folder;
    }

    public async Task<IFileExplorerFolderRange> GetFolderOrRootRangeAsync(NodePath? nodePath, Range range, CancellationToken cancellationToken = default)
    {
        // If no folder is specified return the root folder
        if (nodePath is null || nodePath.IsRoot)
        {
            // TODO: maybe range the root folder? but I doubt it will be needed
            return new FileExplorerFolderRange
            {
                Node = rootFolder.Node,
                Items = rootFolder.Children,
                Sort = FolderSortModel.Default
            };
        }

        // The folder is out of bounds return the root folder instead
        if (!rootFolder.DescendantOfRoot(nodePath))
        {
            throw new ForbiddenRequestException("You do not have permission to access this folder");
        }
        
        var folder = await GetFolderRangeAsync(nodePath, range, cancellationToken);

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

    public async Task SetFolderSortAsync(IFolderSortRequest request, CancellationToken cancellationToken)
    {
        await currentUserRepository.LoadFileSortByAbsolutePathAsync(request.Path, cancellationToken);
        var user = await currentUserRepository.GetCurrentUserAsync();

        var sort = user.FolderSorts.SingleOrDefault(s =>
            s.NodeEntity.RootChild.RelativePath == request.Path.RootPath &&
            s.NodeEntity.RelativePath == request.Path.RelativePath);
        
        // Use the request sort so that its already pre-sorted the new way
        var folderEntity = await fileSystemQueryService.GetFolderNodeOrDefaultAsync(request.Path, GetFolderQueryOptions.FolderOnly, cancellationToken);

        if (folderEntity is null)
        {
            throw new NotFoundException(nameof(MixServerDbContext.Nodes), request.Path.AbsolutePath);
        }

        if (sort is null)
        {
            sort = new FolderSort
            {
                Id = Guid.NewGuid(),
                NodeEntity = folderEntity,
                NodeIdEntity = folderEntity.Id,
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
    }

    private async Task<IFolderSort> GetCurrentUsersFolderSortAsync(NodePath nodePath, CancellationToken cancellationToken)
    {
        if (!currentUserRepository.HasUserId)
        {
            return FolderSortModel.Default;
        }

        await currentUserRepository.LoadFileSortByAbsolutePathAsync(nodePath, cancellationToken);
        return (await currentUserRepository.GetCurrentUserAsync()).GetSortOrDefault(nodePath);
    }
}