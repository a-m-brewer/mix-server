using Microsoft.Extensions.Options;
using MixServer.FolderIndexer.Domain;
using MixServer.FolderIndexer.Domain.Entities;
using MixServer.FolderIndexer.Domain.Repositories;
using MixServer.FolderIndexer.Settings;

namespace MixServer.FolderIndexer.Services;

internal interface IFileSystemRootPersistenceService
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}

internal class FileSystemRootPersistenceService(
    IOptions<FileSystemRootSettings> rootSettings,
    IFileSystemRootRepository fileSystemRootRepository,
    IFileIndexerUnitOfWork unitOfWork)
    : IFileSystemRootPersistenceService
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var existing = await fileSystemRootRepository.GetAllAsync(cancellationToken);

        var allRoots = existing
            .Select(x => x.AbsolutePath)
            .Concat(rootSettings.Value.ChildrenSplit)
            .Distinct()
            .ToHashSet();

        foreach (var root in from root in allRoots
                 join existingRoot in existing on root equals existingRoot.AbsolutePath into existingRootGroup
                 from existingRoot in existingRootGroup.DefaultIfEmpty()
                 join child in rootSettings.Value.ChildrenSplit on root equals child into settingsRootGroup
                 from settingsRoot in settingsRootGroup.DefaultIfEmpty()
                 select new
                 {
                     AbsolutePath = root,
                     ExistingRoot = existingRoot,
                     SettingsRoot = settingsRoot
                 })
        {
            if (root.ExistingRoot is null && !string.IsNullOrWhiteSpace(root.SettingsRoot))
            {
                var rootFolder = new FileSystemRootEntity
                {
                    Id = Guid.NewGuid(),
                    AbsolutePath = root.AbsolutePath
                };
                await fileSystemRootRepository.AddAsync(rootFolder, cancellationToken);
            }
            else if (root.ExistingRoot is not null && string.IsNullOrWhiteSpace(root.SettingsRoot))
            {
                fileSystemRootRepository.Remove(root.ExistingRoot);
            }
            // No need to add an update as the only property is the absolute path
        }
        
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}