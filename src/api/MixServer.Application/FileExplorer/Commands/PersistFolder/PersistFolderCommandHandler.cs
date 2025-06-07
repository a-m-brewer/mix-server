using Microsoft.Extensions.Logging;
using MixServer.Domain.FileExplorer.Converters;
using MixServer.Domain.FileExplorer.Entities;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Services;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Persistence;
using MixServer.Domain.Sessions.Repositories;

namespace MixServer.Application.FileExplorer.Commands.PersistFolder;

public class PersistFolderCommandHandler(
    IFileExplorerEntityConverter fileExplorerEntityConverter,
    IFolderExplorerNodeEntityRepository folderExplorerNodeEntityRepository,
    IFileSystemHashService fileSystemHashService,
    ILogger<PersistFolderCommandHandler> logger,
    IRootFileExplorerFolder rootFolder,
    IUnitOfWork unitOfWork) : ICommandHandler<PersistFolderCommand>
{
    public async Task HandleAsync(PersistFolderCommand request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Received persist folder request for {Directory} with {ChildrenCount} children",
            request.Directory.FullName, request.Children.Count);

        var dirNodePath = rootFolder.GetNodePath(request.Directory.FullName);
        var root = await folderExplorerNodeEntityRepository.GetRootChildOrThrowAsync(dirNodePath.RootPath, cancellationToken);
        var dirHash = await fileSystemHashService.ComputeFolderMd5HashAsync(request.Directory, cancellationToken);

        FileExplorerFolderNodeEntity? parentEntity = null;
        if (dirNodePath.IsRootChild)
        {
            root.Update(dirNodePath, request.Directory, dirHash);
        }
        else
        {
            var parentNodePath = dirNodePath.Parent;
            var parentNodeEntity = parentNodePath.IsRootChild
                ? null
                : await folderExplorerNodeEntityRepository.GetFolderNodeOrDefaultAsync(parentNodePath, false, cancellationToken);
            
            parentEntity = await folderExplorerNodeEntityRepository.GetFolderNodeOrDefaultAsync(dirNodePath, cancellationToken: cancellationToken);
            if (parentEntity is null)
            {
                parentEntity = await fileExplorerEntityConverter.CreateFolderEntityAsync(
                    request.Directory,
                    root,
                    parentNodeEntity,
                    cancellationToken);
                await folderExplorerNodeEntityRepository.AddAsync(parentEntity, cancellationToken);
            }
            else
            {
                parentEntity.Update(dirNodePath, request.Directory, parentNodeEntity, dirHash);
            }
        }

        var fsChildPaths = request
            .Children
            .Select(s => rootFolder.GetNodePath(s.FullName))
            .ToList();
        var childEntities = await folderExplorerNodeEntityRepository
            .GetNodesAsync(
                root.RelativePath,
                fsChildPaths.Select(s => s.RelativePath).ToList(),
                cancellationToken);
        
        foreach (var child in request.Children)
        {
            var childNodePath = fsChildPaths
                .First(f => f.AbsolutePath == child.FullName);
            var childEntity = childEntities
                .FirstOrDefault(f => f.RelativePath == childNodePath.RelativePath);

            if (childEntity is null)
            {
                childEntity = await fileExplorerEntityConverter.CreateNodeAsync(
                    child,
                    root,
                    parentEntity,
                    cancellationToken);
                await folderExplorerNodeEntityRepository.AddAsync(childEntity, cancellationToken);
                childEntities.Add(childEntity);
            }
            else 
            { 
                var hash = child is DirectoryInfo childDir
                    ? await fileSystemHashService.ComputeFolderMd5HashAsync(childDir, cancellationToken)
                    : await fileSystemHashService.ComputeFileMd5HashAsync(childNodePath, cancellationToken);
                childEntity.Update(childNodePath, child, parentEntity, hash);
            }
        }

        folderExplorerNodeEntityRepository.RemoveRange(childEntities
            .Where(w => fsChildPaths.All(a => a.RelativePath != w.RelativePath)));
        if (parentEntity is not null)
        {
            folderExplorerNodeEntityRepository.RemoveRange(parentEntity.Children
                .Where(w => fsChildPaths.All(a => a.RelativePath != w.RelativePath)));
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}