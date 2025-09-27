using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MixServer.Domain.Exceptions;
using MixServer.Domain.Extensions;
using MixServer.Domain.FileExplorer.Entities;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Repositories;
using MixServer.Domain.FileExplorer.Services;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Persistence;

namespace MixServer.Application.FileExplorer.Commands.ScanFolder;

public class ScanFolderCommandHandler(
    IFolderScanTrackingStore folderScanTrackingStore,
    ILogger<ScanFolderCommandHandler> logger,
    IRootFileExplorerFolder rootFolder,
    IServiceProvider serviceProvider,
    IUpdateMediaMetadataChannel updateMediaMetadataChannel) : ICommandHandler<ScanFolderRequest>
{
    public async Task HandleAsync(ScanFolderRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Received scan folder request for {NodePath} with recursive={Recursive}", request.NodePath, request.Recursive);

        if (request.NodePath.IsRoot)
        {
            throw new InvalidRequestException(nameof(request.NodePath), "Cannot scan the root folder.");
        }

        if (!request.NodePath.IsDirectory)
        {
            throw new InvalidRequestException(nameof(request.NodePath), "The specified path is not a directory.");
        }
        
        // using var scope = serviceProvider.CreateScope();
        // var fileExplorerNodeRepository = scope.ServiceProvider.GetRequiredService<IFileExplorerNodeRepository>();

        // var fsHeader = new FolderHeader
        // {
        //     NodePath = request.NodePath,
        //     Hash = await fileSystemHashService.ComputeFolderMd5HashAsync(request.NodePath, cancellationToken)
        // };
        // var dbHeader = await fileExplorerNodeRepository.GetFolderHeaderOrDefaultAsync(fsHeader, cancellationToken);
        //
        // var folderDiff = new FolderDiff
        // {
        //     FileSystemHeader = fsHeader,
        //     DatabaseHeader = dbHeader
        // };

        // TODO: reenable
        // if (!folderDiff.Dirty)
        // {
        //     logger.LogInformation("Folder {NodePath} is already up to date ({ExpectedHash}). Skipping...", request.NodePath, fsHeader);
        //     return;
        // }

        folderScanTrackingStore.ScanInProgress = true;

        var dirInfo = new DirectoryInfo(request.NodePath.AbsolutePath);
        
        await RunScanAsync(dirInfo, request.Recursive, cancellationToken);
    }

    private async Task RunScanAsync(DirectoryInfo root, bool recursive, CancellationToken cancellationToken)
    {
        var nodePath = rootFolder.GetNodePath(root.FullName);
        
        List<DirectoryInfo> childNodes;
        try
        {
            var folders = await ProcessDirectoryAsync(nodePath, root, cancellationToken);
            childNodes = folders;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to persist folder {NodePath}", nodePath);
            return;
        }

        if (!recursive || childNodes.Count == 0)
        {
            return;
        }

        var actionBlock = new ActionBlock<int>(async i =>
            {
                await RunScanAsync(childNodes[i], recursive, cancellationToken).ConfigureAwait(false);
            },
            new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount
            });

        logger.LogInformation("Sending {Count} child nodes for scanning in parallel", childNodes.Count);
        for (var i = 0; i < childNodes.Count; i++)
        {
            await actionBlock.SendAsync(i, cancellationToken).ConfigureAwait(false);
        }

        actionBlock.Complete();

        await actionBlock.Completion.ConfigureAwait(false);
    }

    private async Task<List<DirectoryInfo>> ProcessDirectoryAsync(NodePath nodePath, DirectoryInfo root, CancellationToken cancellationToken)
    {
        await ExecuteScopedAndSaveChangesAsync(async (service, token) =>
        {
            await service.EnsureParentUpdatedAsync(nodePath, root, token);
        }, cancellationToken);

        var hashBuilder = new FsHashBuilder();
        var directories = new List<DirectoryInfo>();

        var page = 0;
        var hasMore = true;

        while (hasMore)
        {
            var (blockHasMore, dirs) = await ExecuteScopedAndSaveChangesAsync(async (service, token) =>
            {
                var fsChildrenChunk = await service.EnsureChildrenUpdatedAsync(nodePath, root, new Page
                {
                    PageIndex = page,
                    PageSize = 500
                }, hashBuilder, token);

                return (fsChildrenChunk.Count > 0, fsChildrenChunk.OfType<DirectoryInfo>());
            }, cancellationToken);
            
            hasMore = blockHasMore;
            directories.AddRange(dirs);
            
            page++;
        }
        
        var hash = hashBuilder.ComputeHash();
        
        await ExecuteScopedAndSaveChangesAsync(async (service, token) =>
        {
            await service.UpdateHashAsync(nodePath, hash, token);
        }, cancellationToken);

        await updateMediaMetadataChannel.WriteAsync(new UpdateMediaMetadataRequest(nodePath), cancellationToken);
        
        logger.LogInformation("Scanned folder {NodePath}", nodePath);
        return directories;
    }

    private async Task ExecuteScopedAndSaveChangesAsync(Func<IFolderPersistenceService, CancellationToken, Task> task, CancellationToken cancellationToken)
    {
        await ExecuteScopedAndSaveChangesAsync(async (service, token) =>
        {
            await task(service, token);
            return true;
        }, cancellationToken);
    }
    
    private async Task<TResult> ExecuteScopedAndSaveChangesAsync<TResult>(Func<IFolderPersistenceService, CancellationToken, Task<TResult>> task, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        
        var res = await task(scope.ServiceProvider.GetRequiredService<IFolderPersistenceService>(), cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        
        return res;
    }
}