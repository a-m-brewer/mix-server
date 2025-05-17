using Microsoft.Extensions.Options;
using MixServer.FolderIndexer.Converters;
using MixServer.FolderIndexer.Domain;
using MixServer.FolderIndexer.Domain.Repositories;
using MixServer.FolderIndexer.Settings;

namespace MixServer.FolderIndexer.Services;

internal interface IFileSystemRootPersistenceService
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}

internal class FileSystemRootPersistenceService(
    IOptions<FileSystemRootSettings> rootSettings,
    IFileSystemInfoConverter fileSystemInfoConverter,
    IFileSystemInfoRepository fileSystemInfoRepository,
    IFileIndexerUnitOfWork unitOfWork)
    : IFileSystemRootPersistenceService
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var existing = await fileSystemInfoRepository.GetAllRootFoldersAsync(cancellationToken);

        var allRoots = existing
            .Select(x => x.RelativePath)
            .Concat(rootSettings.Value.ChildrenSplit)
            .Distinct()
            .ToHashSet();

        foreach (var root in from root in allRoots
                 join existingRoot in existing on root equals existingRoot.RelativePath into existingRootGroup
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
            var directoryInfo = new DirectoryInfo(root.AbsolutePath);
            // Add
            if (root.ExistingRoot is null && !string.IsNullOrWhiteSpace(root.SettingsRoot))
            {
                var rootFolder = fileSystemInfoConverter.ConvertRoot(directoryInfo);
                await fileSystemInfoRepository.AddAsync(rootFolder, cancellationToken);
            }
            // Remove
            else if (root.ExistingRoot is not null && string.IsNullOrWhiteSpace(root.SettingsRoot))
            {
                fileSystemInfoRepository.Remove(root.ExistingRoot);
            }
            // Update
            else if (root.ExistingRoot is not null && !string.IsNullOrWhiteSpace(root.SettingsRoot))
            {
                fileSystemInfoConverter.UpdateRoot(root.ExistingRoot, directoryInfo);
            }
        }
        
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}