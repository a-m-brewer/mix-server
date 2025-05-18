using Microsoft.Extensions.Logging;
using MixServer.FolderIndexer.Converters;
using MixServer.FolderIndexer.Domain;
using MixServer.FolderIndexer.Domain.Entities;
using MixServer.FolderIndexer.Domain.Repositories;

namespace MixServer.FolderIndexer.Services;

internal interface IFileSystemPersistenceService
{
    Task AddOrUpdateFolderAsync(DirectoryInfo directoryInfo, ICollection<FileSystemInfo> children, CancellationToken cancellationToken = default);
}

internal class FileSystemPersistenceService(
    IFileSystemInfoRepository fileSystemInfoRepository,
    IFileSystemInfoConverter fileSystemInfoConverter,
    ILogger<FileSystemPersistenceService> logger,
    IFileIndexerUnitOfWork unitOfWork)
    : IFileSystemPersistenceService
{
    public async Task AddOrUpdateFolderAsync(
        DirectoryInfo directoryInfo,
        ICollection<FileSystemInfo> children,
        CancellationToken cancellationToken = default)
    {
        var dirs = await fileSystemInfoRepository.GetDirectoriesAsync<DirectoryInfoEntity>(directoryInfo.FullName, cancellationToken);
        
        logger.LogInformation("{FullName} - Root: {Root} - Parent: {Parent} - Directory: {Directory}",
            directoryInfo.FullName,
            dirs.Root.RelativePath,
            dirs.Parent?.RelativePath,
            dirs.Entity?.RelativePath);

        var dir = dirs.Entity;
        if (dir is null)
        {
            dir = fileSystemInfoConverter.ConvertChildDirectory(directoryInfo, dirs.Root, dirs.Parent);
            await fileSystemInfoRepository.AddAsync(dir, cancellationToken);
        }
        else if (dir is not RootDirectoryInfoEntity)
        {
            fileSystemInfoConverter.UpdateChildDirectory(dir, directoryInfo, dirs.Root, dirs.Parent);
        }
        

        var relativePaths = children.Select(s => Path.GetRelativePath(dirs.Root.RelativePath, s.FullName))
            .Concat(dir.Children.Select(s => s.RelativePath))
            .Distinct()
            .ToList();

        foreach (var child in from relativePath in relativePaths
                 join dbChild in dir.Children on relativePath equals dbChild.RelativePath into dbChildGroup
                 from dbChild in dbChildGroup.DefaultIfEmpty()
                 join fsChild in children on relativePath equals Path.GetRelativePath(dirs.Root.RelativePath, fsChild.FullName) into fsChildGroup
                 from settingsRoot in fsChildGroup.DefaultIfEmpty()
                 select new
                 {
                     AbsolutePath = relativePath,
                     Db = dbChild,
                     Fs = settingsRoot
                 })
        {
            // Add
            if (child.Db is null && child.Fs is not null)
            {
                var fsEntity = fileSystemInfoConverter.ConvertChild(child.Fs, dirs.Root, dir);
                fsEntity.Parent = dir;
                await fileSystemInfoRepository.AddAsync(fsEntity, cancellationToken);
            }
            // Remove
            else if (child.Db is not null && child.Fs is null)
            {
                fileSystemInfoRepository.Remove(child.Db);
            }
            // Update
            else if (child.Db is not null && child.Fs is not null)
            {
                fileSystemInfoConverter.UpdateChild(child.Db, child.Fs, dirs.Root, dir);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}