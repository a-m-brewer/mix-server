using MixServer.FolderIndexer.Interface.Models;

namespace MixServer.FolderIndexer.Interface.Api;

public interface IFolderIndexerFileSystemApi
{
    Task<IDirectoryInfo> GetDirectoryInfoAsync(string absolutePath, CancellationToken cancellationToken = default);
    Task<IFileInfo> GetFileInfoAsync(string absolutePath, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<IRootDirectoryInfo>> GetRootDirectoriesAsync();
}