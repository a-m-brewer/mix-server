using Microsoft.EntityFrameworkCore;
using MixServer.Domain.FileExplorer.Entities;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Services;
using MixServer.Infrastructure.EF;
using MixServer.Infrastructure.EF.Extensions;

namespace MixServer.Services;
#pragma warning disable CS0618 // Type or member is obsolete - used for migration purposes
public class AbsolutePathMigrationService(
    MixServerDbContext context,
    IFileSystemHashService fileSystemHashService,
    ILogger<AbsolutePathMigrationService> logger,
    IRootFileExplorerFolder rootFolder)
{
    public async Task MigrateAsync()
    {
        logger.LogInformation("Starting absolute path migration...");

        var rootChildNodes = await EnsureRootChildNodesExistAsync();

        await MigratePlaybackSessionsAsync(rootChildNodes);
        await MigrateFolderSortsAsync(rootChildNodes);
        await MigrateTranscodesAsync(rootChildNodes);
        
        logger.LogInformation("Absolute path migration completed successfully");
    }

    private async Task<Dictionary<string, FileExplorerRootChildNodeEntity>> EnsureRootChildNodesExistAsync()
    {
        logger.LogInformation("Creating root child nodes if they do not exist...");
        var rootChildNodes = await context.Nodes
            .OfType<FileExplorerRootChildNodeEntity>()
            .ToDictionaryAsync(k => k.RelativePath, v => v);
        foreach (var rootChild in rootFolder.Children.Where(w => !rootChildNodes.ContainsKey(w.Path.RootPath)))
        {
            var rootChildEntity = new FileExplorerRootChildNodeEntity
            {
                Id = Guid.NewGuid(),
                RelativePath = rootChild.Path.RootPath,
                Exists = rootChild.Exists,
                CreationTimeUtc = rootChild.CreationTimeUtc
            };
            await context.Nodes.AddAsync(rootChildEntity);
            rootChildNodes[rootChild.Path.RootPath] = rootChildEntity;
            logger.LogInformation("Created root child node for path {RootPath}", rootChild.Path.RootPath);
        }
        await context.SaveChangesAsync();
        logger.LogInformation("Root child nodes creation completed");
        
        return rootChildNodes;
    }
    
    private async Task MigratePlaybackSessionsAsync(Dictionary<string, FileExplorerRootChildNodeEntity> rootChildNodes)
    {
        logger.LogInformation("Clearing absolute paths in playback sessions...");
        await foreach (var session in context.PlaybackSessions
                           .IncludeNode()
                           .Where(w => w.AbsolutePath != string.Empty)
                           .AsAsyncEnumerable())
        {
            logger.LogInformation("Processing session {SessionId} with absolute path {AbsolutePath}", session.Id, session.AbsolutePath);
            var nodePath = rootFolder.GetNodePath(session.AbsolutePath);
            
            var file = await context.Nodes.OfType<FileExplorerFileNodeEntity>()
                .Include(i => i.RootChild)
                .FirstOrDefaultAsync(f => f.RootChild.RelativePath == nodePath.RootPath && f.RelativePath == nodePath.RelativePath);

            if (file is null)
            {
                logger.LogInformation("File not found for session {SessionId} with absolute path {AbsolutePath}", session.Id, session.AbsolutePath);
                
                var root = rootChildNodes[nodePath.RootPath];
                var fileInfo = new FileInfo(nodePath.AbsolutePath);
                
                file = new FileExplorerFileNodeEntity
                {
                    Id = Guid.NewGuid(),
                    RelativePath = nodePath.RelativePath,
                    RootChild = root,
                    Exists = fileInfo.Exists,
                    CreationTimeUtc = fileInfo.CreationTimeUtc,
                    Parent = null 
                };
                await context.Nodes.AddAsync(file);
            }
            else
            {
                var root = rootChildNodes[nodePath.RootPath];
                if (file.RootChild.Id != root.Id)
                {
                    logger.LogInformation("Updating file {FileId} root child from {OldRootId} to {NewRootId}", file.Id, file.RootChild.Id, root.Id);
                    file.RootChild = root;
                }
            }

            session.Node = file;
            session.NodeId = file.Id;
            session.AbsolutePath = string.Empty; // Clear the absolute path after migration to indicate it has been processed.
            logger.LogInformation("Session {SessionId} processed successfully", session.Id);
        }
        
        await context.SaveChangesAsync();
        logger.LogInformation("Finished clearing absolute paths in playback sessions");
    }
    
    private async Task MigrateFolderSortsAsync(Dictionary<string, FileExplorerRootChildNodeEntity> rootChildNodes)
    {
        logger.LogInformation("Clearing absolute paths in folder sorts...");

        await foreach (var sort in context.FolderSorts
                     .IncludeNode()
                     .Where(w => w.AbsoluteFolderPath != string.Empty)
                     .AsAsyncEnumerable())
        {
            logger.LogInformation("Processing folder sort {FolderSortId} with absolute path {AbsolutePath}", sort.Id, sort.AbsoluteFolderPath);
            var nodePath = rootFolder.GetNodePath(sort.AbsoluteFolderPath);
            
            var folder = await context.Nodes.OfType<FileExplorerFolderNodeEntity>()
                .Include(i => i.RootChild)
                .FirstOrDefaultAsync(f => f.RootChild.RelativePath == nodePath.RootPath && f.RelativePath == nodePath.RelativePath);

            if (folder is null)
            {
                logger.LogInformation("Folder not found for sort {FolderSortId} with absolute path {AbsolutePath}", sort.Id, sort.AbsoluteFolderPath);
                
                var root = rootChildNodes[nodePath.RootPath];
                var directoryInfo = new DirectoryInfo(nodePath.AbsolutePath);
                folder = new FileExplorerFolderNodeEntity
                {
                    Id = Guid.NewGuid(),
                    RelativePath = nodePath.RelativePath,
                    RootChild = root,
                    Exists = directoryInfo.Exists,
                    CreationTimeUtc = directoryInfo.CreationTimeUtc,
                    Parent = null
                };
                await context.Nodes.AddAsync(folder);
            }
            else
            {
                var root = rootChildNodes[nodePath.RootPath];
                if (folder.RootChildId != root.Id)
                {
                    logger.LogInformation("Updating folder {FolderId} root child to {RootChildId}", folder.Id, rootChildNodes[nodePath.RootPath].Id);
                    folder.RootChild = root;
                }
            }

            sort.Node = folder;
            sort.NodeId = folder.Id;
            sort.AbsoluteFolderPath = string.Empty; // Clear the absolute path after migration to indicate it has been processed.
        }

        await context.SaveChangesAsync();
        logger.LogInformation("Finished clearing absolute paths in folder sorts");
    }

    private async Task MigrateTranscodesAsync(Dictionary<string, FileExplorerRootChildNodeEntity> rootChildNodes)
    {
        logger.LogInformation("Clearing absolute paths in transcodes...");
        await foreach (var transcode in context.Transcodes
                           .IncludeNode()
                           .Where(w => w.AbsolutePath != string.Empty)
                           .AsAsyncEnumerable())
        {
            logger.LogInformation("Processing transcode {TranscodeId} with absolute path {AbsolutePath}", transcode.Id,
                transcode.AbsolutePath);

            var nodePath = rootFolder.GetNodePath(transcode.AbsolutePath);

            var file = await context.Nodes.OfType<FileExplorerFileNodeEntity>()
                .Include(i => i.RootChild)
                .FirstOrDefaultAsync(f =>
                    f.RootChild.RelativePath == nodePath.RootPath && f.RelativePath == nodePath.RelativePath);

            if (file is null)
            {
                logger.LogInformation("File not found for transcode {TranscodeId} with absolute path {AbsolutePath}",
                    transcode.Id, transcode.AbsolutePath);
                var root = rootChildNodes[nodePath.RootPath];
                var fileInfo = new FileInfo(nodePath.AbsolutePath);
                file = new FileExplorerFileNodeEntity
                {
                    Id = Guid.NewGuid(),
                    RelativePath = nodePath.RelativePath,
                    RootChild = root,
                    Exists = fileInfo.Exists,
                    CreationTimeUtc = fileInfo.CreationTimeUtc,
                    Hash = await fileSystemHashService.ComputeFileMd5HashAsync(nodePath),
                    Parent = null
                };
                await context.Nodes.AddAsync(file);
            }
            else
            {
                var root = rootChildNodes[nodePath.RootPath];
                if (file.RootChildId != root.Id)
                {
                    logger.LogInformation("Updating transcode {TranscodeId} root child from {OldRootId} to {NewRootId}",
                        transcode.Id, file.RootChild.Id, root.Id);
                    file.RootChild = root;
                }
            }
            
            transcode.Node = file;
            transcode.NodeId = file.Id;
            transcode.AbsolutePath = string.Empty; // Clear the absolute path after migration to indicate it has been processed.
        }
        
        await context.SaveChangesAsync();
        logger.LogInformation("Finished clearing absolute paths in transcodes");
    }
}
#pragma warning restore CS0618 // Type or member is obsolete