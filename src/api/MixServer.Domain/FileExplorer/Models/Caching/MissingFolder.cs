namespace MixServer.Domain.FileExplorer.Models.Caching;

public class MissingFolder(string absolutePath) : IFolder
{
    public ICacheDirectoryInfo DirectoryInfo { get; } = new CacheDirectoryInfo(new DirectoryInfo(absolutePath));
    public IReadOnlyCollection<ICacheDirectoryInfo> Directories { get; } = new List<ICacheDirectoryInfo>();
    public IReadOnlyCollection<ICacheFileInfo> Files { get; } = new List<ICacheFileInfo>();
}