using MixServer.Domain.Exceptions;
using MixServer.Domain.FileExplorer.Converters;
using MixServer.Domain.FileExplorer.Entities;
using MixServer.Domain.FileExplorer.Enums;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Repositories;
using MixServer.Domain.FileExplorer.Services;
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
    public async Task<IFileExplorerFolderPage> GetFolderPageAsync(NodePath nodePath, Page page, CancellationToken cancellationToken = default)
    {
        var sort = await GetCurrentUsersFolderSortAsync(nodePath, cancellationToken);

        var folderEntity = await folderPersistenceService.GetOrAddFolderAsync(nodePath, page, sort, cancellationToken: cancellationToken);
        
        var folder = fileExplorerEntityConverter.ConvertPage(folderEntity, page, sort);

        return folder;
    }

    public async Task<IFileExplorerFolderPage> GetFolderOrRootPageAsync(NodePath? nodePath, Page page, CancellationToken cancellationToken = default)
    {
        // If no folder is specified return the root folder
        if (nodePath is null || nodePath.IsRoot)
        {
            // TODO: maybe page the root folder? but I doubt it will be needed
            return new FileExplorerFolderPage
            {
                Node = rootFolder.Node,
                Page = new FileExplorerFolderChildPage
                {
                    PageIndex = 0,
                    Children = rootFolder.Children
                },
                Sort = FolderSortModel.Default
            };
        }

        // The folder is out of bounds return the root folder instead
        if (!rootFolder.DescendantOfRoot(nodePath))
        {
            throw new ForbiddenRequestException("You do not have permission to access this folder");
        }
        
        var folder = await GetFolderPageAsync(nodePath, page, cancellationToken);

        return folder.Node.Exists
            ? folder
            : throw new NotFoundException("Folder", nodePath.AbsolutePath);
    }

    public async Task<IFileExplorerFolder> GetFolderAsync(NodePath nodePath,
        Page page,
        CancellationToken cancellationToken = default)
    {
        var sort = await GetCurrentUsersFolderSortAsync(nodePath, cancellationToken);

        var folderEntity = await folderPersistenceService.GetOrAddFolderAsync(nodePath, page, sort, cancellationToken: cancellationToken);
        var folder = fileExplorerEntityConverter.Convert(folderEntity);
        
        // TODO: remove this as the folder no longer needs to do the sorting itself
        folder.Sort = sort;

        return folder;
    }

    public async Task<IFileExplorerFolder> GetFolderOrRootAsync(NodePath? nodePath,
        Page page,
        CancellationToken cancellationToken = default)
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
        
        var folder = await GetFolderAsync(nodePath, page, cancellationToken);

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

    public async Task<(IFileExplorerFolder Parent, IFileExplorerFileNode File)> GetFileAndFolderAsync(NodePath nodePath, CancellationToken cancellationToken)
    {
        // TODO: paging
        var folder = await GetFolderOrRootAsync(nodePath.Parent, new Page
        {
            PageIndex = 0,
            PageSize = 25
        }, cancellationToken);
        
        var file = folder
                       .Children
                       .OfType<IFileExplorerFileNode>()
                       .SingleOrDefault(f => f.Path.RootPath == nodePath.RootPath && f.Path.RelativePath == nodePath.RelativePath) ??
                   throw new NotFoundException(nodePath.Parent.AbsolutePath, nodePath.FileName);

        return (folder, file);
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

    public async Task<IFileExplorerFolderPage> SetFolderSortAsync(IFolderSortRequest request, CancellationToken cancellationToken)
    {
        await currentUserRepository.LoadFileSortByAbsolutePathAsync(request.Path, cancellationToken);
        var user = await currentUserRepository.GetCurrentUserAsync();

        var sort = user.FolderSorts.SingleOrDefault(s =>
            s.NodeEntity.RootChild.RelativePath == request.Path.RootPath &&
            s.NodeEntity.RelativePath == request.Path.RelativePath);
        
        
        // Reset paging to 0, as the sort will change the order of the items
        var page = new Page
        {
            PageIndex = 0,
            PageSize = request.PageSize
        };
        
        // Use the request sort so that its already pre-sorted the new way
        var folderEntity = await folderPersistenceService.GetOrAddFolderAsync(request.Path, page, request, cancellationToken: cancellationToken);

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

        var folderPage = fileExplorerEntityConverter.ConvertPage(folderEntity, page, sort);
        
        return folderPage;
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