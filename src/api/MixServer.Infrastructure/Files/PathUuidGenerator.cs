using MixServer.Domain.FileExplorer.Services;
using UUIDNext;

namespace MixServer.Infrastructure.Files;

public class PathUuidGenerator : IPathUuidGenerator
{
    private readonly Guid _uriNamespace = Guid.Parse("6ba7b811-9dad-11d1-80b4-00c04fd430c8");
    
    public Guid GetUuidForPath(string path)
    {
        return Uuid.NewNameBased(_uriNamespace, new Uri(path).AbsoluteUri);
    }
}