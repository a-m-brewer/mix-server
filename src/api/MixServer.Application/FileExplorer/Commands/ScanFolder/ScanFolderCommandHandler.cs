using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Logging;
using MixServer.Domain.Exceptions;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Repositories;
using MixServer.Domain.FileExplorer.Services;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Sessions.Repositories;

namespace MixServer.Application.FileExplorer.Commands.ScanFolder;

public class ScanFolderCommandHandler(
    IFolderExplorerNodeEntityRepository folderExplorerNodeEntityRepository,
    IFileSystemHashService fileSystemHashService,
    ILogger<ScanFolderCommandHandler> logger,
    IPersistFolderCommandChannel persistFolderCommandChannel,
    IRootFileExplorerFolder rootFolder) : ICommandHandler<ScanFolderRequest>
{
    private static readonly EnumerationOptions EnumerationOptions = new()
    {
        RecurseSubdirectories = false,
        IgnoreInaccessible = true,
        AttributesToSkip = 0
    };
    
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
        
        var expectedHash = await fileSystemHashService.ComputeFolderMd5HashAsync(request.NodePath, cancellationToken);
        var actualHash = await folderExplorerNodeEntityRepository.GetHashOrDefaultAsync(request.NodePath, cancellationToken);

        if (expectedHash == actualHash)
        {
            logger.LogInformation("Folder {NodePath} is already up to date ({ExpectedHash}). Skipping...", request.NodePath, expectedHash);
            return;
        }

        await RunScanAsync(request.NodePath, request.Recursive, cancellationToken);
    }

    private async Task RunScanAsync(NodePath path, bool recursive, CancellationToken cancellationToken)
    {
        var root = new DirectoryInfo(path.AbsolutePath);

        List<FileSystemInfo> children;
        try
        {
            children = root.EnumerateFileSystemInfos("*", EnumerationOptions).ToList();
        }
        catch
        {
            return;
        }
        
        if (children.Count == 0)
        {
            return;
        }
        
        _ = persistFolderCommandChannel.WriteAsync(new PersistFolderCommand
        {
            Directory = root,
            Children = children
        }, cancellationToken);
        
        if (!recursive)
        {
            return;
        }
        
        var childNodePaths = children.OfType<DirectoryInfo>()
            .Select(s => rootFolder.GetNodePath(s.FullName))
            .ToList();
        
        if (childNodePaths.Count == 0)
        {
            return;
        }
        
        var actualHashes = await folderExplorerNodeEntityRepository.GetHashesAsync(childNodePaths, cancellationToken);
        var actualHashTasks =
            childNodePaths.Select(async cnp => new KeyValuePair<NodePath, string?>(cnp, await fileSystemHashService.ComputeFolderMd5HashAsync(cnp, cancellationToken)));
        var expectedHashes = (await Task.WhenAll(actualHashTasks))
            .ToDictionary(k => k.Key, v => v.Value);

        var folders = new List<NodePath>();
        foreach (var childNodePath in childNodePaths)
        {
            if (!expectedHashes.TryGetValue(childNodePath, out var expectedHash))
            {
                logger.LogWarning("Failed to compute hash for {NodePath}. Skipping...", childNodePath);
                continue;
            }
            
            var actualHash = actualHashes.GetValueOrDefault(childNodePath);
            
            if (expectedHash == actualHash)
            {
                logger.LogInformation("Folder {NodePath} is already up to date ({ExpectedHash}). Skipping...", childNodePath, expectedHash);
                continue;
            }

            folders.Add(childNodePath);
        }

        var actionBlock = new ActionBlock<int>(async i =>
            {
                await RunScanAsync(folders[i], recursive, cancellationToken).ConfigureAwait(false);
            },
            new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount
            });

        for (var i = 0; i < folders.Count; i++)
        {
            await actionBlock.SendAsync(i, cancellationToken).ConfigureAwait(false);
        }

        actionBlock.Complete();

        await actionBlock.Completion.ConfigureAwait(false);
    }
}