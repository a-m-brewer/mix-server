using MixServer.Domain.FileExplorer.Entities;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Services;
using MixServer.Domain.Interfaces;

namespace MixServer.Domain.FileExplorer.Converters;

public interface IFileExplorerEntityConverter : IConverter
{
    Task<FileExplorerRootChildNodeEntity> CreateRootChildEntityAsync(DirectoryInfo directoryInfo);
}

public class FileExplorerEntityConverter(IFileSystemHashService fileSystemHashService) : IFileExplorerEntityConverter
{
    public async Task<FileExplorerRootChildNodeEntity> CreateRootChildEntityAsync(DirectoryInfo directoryInfo)
    {
        return new FileExplorerRootChildNodeEntity
        {
            Id = Guid.NewGuid(),
            RelativePath = directoryInfo.FullName,
            Exists = directoryInfo.Exists,
            CreationTimeUtc = directoryInfo.CreationTimeUtc,
            Hash = await fileSystemHashService.ComputeFolderMd5HashAsync(new NodePath(directoryInfo.FullName, string.Empty))
        };
    }
}