namespace MixServer.Domain.FileExplorer.Services;

public interface IPathUuidGenerator
{
    Guid GetUuidForPath(string path);
}