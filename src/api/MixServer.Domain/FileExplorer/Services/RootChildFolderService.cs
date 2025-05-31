using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MixServer.Domain.FileExplorer.Converters;
using MixServer.Domain.FileExplorer.Entities;
using MixServer.Domain.FileExplorer.Settings;
using MixServer.Domain.Persistence;
using MixServer.Domain.Sessions.Repositories;

namespace MixServer.Domain.FileExplorer.Services;

public interface IRootChildFolderService
{
    Task<ICollection<FileExplorerRootChildNodeEntity>> SyncRootChildFoldersAsync(CancellationToken cancellationToken);
}

public class RootChildFolderService(
    IFileExplorerEntityConverter fileExplorerEntityConverter,
    IFolderExplorerNodeEntityRepository folderExplorerNodeRepository,
    ILogger<RootChildFolderService> logger,
    IOptions<RootFolderSettings> rootFolderSettings,
    IUnitOfWork unitOfWork) : IRootChildFolderService
{
    public async Task<ICollection<FileExplorerRootChildNodeEntity>> SyncRootChildFoldersAsync(CancellationToken cancellationToken)
    {
        var configRootFolders = rootFolderSettings.Value.ChildrenSplit.ToList();
        var dbRootChildren = await folderExplorerNodeRepository.GetAllRootChildrenAsync(cancellationToken);

        var allPaths = dbRootChildren
            .Select(f => f.RelativePath)
            .Union(configRootFolders)
            .Distinct()
            .ToList();

        var rootChildren = new List<FileExplorerRootChildNodeEntity>();

        foreach (var path in allPaths)
        {
            var configPath = configRootFolders.FirstOrDefault(f => f == path);
            var dbPath = dbRootChildren.FirstOrDefault(f => f.RelativePath == path);

            if (dbPath is null && configPath is not null)
            {
                logger.LogInformation("Adding new root child folder: {Path}", configPath);
                var rootChildDirectoryInfo = new DirectoryInfo(configPath);
                var rootChild = fileExplorerEntityConverter.CreateRootChildEntityAsync(rootChildDirectoryInfo);
                await folderExplorerNodeRepository.AddAsync(rootChild, cancellationToken);
                
                rootChildren.Add(rootChild);
            }
            else if (dbPath is not null && configPath is null)
            {
                logger.LogInformation("Root not found in config, hiding root child folder: {Path}", dbPath.RelativePath);
                dbPath.Hidden = true; // Mark as hidden instead of deleting, so that it can easily be restored later
            }
            else if (dbPath is not null && configPath is not null)
            {
                logger.LogInformation("Root child folder exists in both config and DB: {Path}", dbPath.RelativePath);
                rootChildren.Add(dbPath);

                if (!dbPath.Hidden)
                {
                    continue;
                }

                logger.LogInformation("Restoring hidden root child folder: {Path}", dbPath.RelativePath);
                dbPath.Hidden = false; // Restore hidden status
            }
        }
        
        await unitOfWork.SaveChangesAsync(cancellationToken);
        
        return rootChildren;
    }
}