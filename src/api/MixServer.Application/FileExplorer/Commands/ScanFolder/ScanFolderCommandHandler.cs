using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Logging;
using MixServer.Domain.Exceptions;
using MixServer.Domain.Extensions;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Repositories;
using MixServer.Domain.FileExplorer.Services;
using MixServer.Domain.Interfaces;

namespace MixServer.Application.FileExplorer.Commands.ScanFolder;

public class ScanFolderCommandHandler(
    IFileExplorerNodeRepository fileExplorerNodeRepository,
    IFileSystemHashService fileSystemHashService,
    ILogger<ScanFolderCommandHandler> logger,
    IPersistFolderCommandChannel persistFolderCommandChannel,
    IRootFileExplorerFolder rootFolder) : ICommandHandler<ScanFolderRequest>
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

        var fsHeader = new FolderHeader
        {
            NodePath = request.NodePath,
            Hash = await fileSystemHashService.ComputeFolderMd5HashAsync(request.NodePath, cancellationToken)
        };
        var dbHeader = await fileExplorerNodeRepository.GetFolderHeaderOrDefaultAsync(fsHeader, cancellationToken);

        var folderDiff = new FolderDiff
        {
            FileSystemHeader = fsHeader,
            DatabaseHeader = dbHeader
        };

        // TODO: reenable
        // if (!folderDiff.Dirty)
        // {
        //     logger.LogInformation("Folder {NodePath} is already up to date ({ExpectedHash}). Skipping...", request.NodePath, fsHeader);
        //     return;
        // }

        await RunScanAsync(folderDiff, request.Recursive, cancellationToken);
    }

    private async Task RunScanAsync(FolderDiff diff, bool recursive, CancellationToken cancellationToken)
    {
        var root = new DirectoryInfo(diff.FileSystemHeader.NodePath.AbsolutePath);

        List<FileSystemInfo> children;
        try
        {
            children = root.MsEnumerateFileSystemInfos().ToList();
        }
        catch
        {
            return;
        }
        
        _ = persistFolderCommandChannel.WriteAsync(new PersistFolderCommand
        {
            DirectoryPath = diff.FileSystemHeader.NodePath,
            Directory = root,
            Children = children
        }, cancellationToken);
        
        if (!recursive || children.Count == 0)
        {
            return;
        }
        
        var childNodes = children.OfType<DirectoryInfo>()
            .Select(s => new { NodePath = rootFolder.GetNodePath(s.FullName), DirectoryInfo = s})
            .ToDictionary(k => k.NodePath, v => v.DirectoryInfo);
        var childNodePaths = childNodes.Keys.ToList();
        
        
        if (childNodes.Count == 0)
        {
            return;
        }
        
        var actualHashes = await fileExplorerNodeRepository.GetFolderHeadersAsync(childNodePaths, cancellationToken);
        var expectedFolderHeaders =
            childNodes.Select(async cnp => new FolderHeader
            {
                NodePath = cnp.Key,
                Hash = await fileSystemHashService.ComputeFolderMd5HashAsync(cnp.Value, cancellationToken)
            });
        var expectedHashes = (await Task.WhenAll(expectedFolderHeaders))
            .ToDictionary(k => k.NodePath);

        var folders = new List<FolderDiff>();
        foreach (var childNodePath in childNodePaths)
        {
            if (!expectedHashes.TryGetValue(childNodePath, out var fsHeader))
            {
                logger.LogWarning("Failed to compute hash for {NodePath}. Skipping...", childNodePath);
                continue;
            }
            
            var dbHeader = actualHashes.GetValueOrDefault(childNodePath);
            
            if (fsHeader.Equals(dbHeader))
            {
                logger.LogInformation("Folder {NodePath} is already up to date ({ExpectedHash}). Skipping...", childNodePath, fsHeader);
                continue;
            }

            folders.Add(new FolderDiff
            {
                FileSystemHeader = fsHeader,
                DatabaseHeader = dbHeader
            });
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