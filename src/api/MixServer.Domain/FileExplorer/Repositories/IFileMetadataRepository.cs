using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.Persistence;

namespace MixServer.Domain.FileExplorer.Repositories;

public interface IFileMetadataRepository : ITransientRepository
{
    public Task AddRangeAsync(ICollection<AddMediaMetadataRequest> metadata, CancellationToken cancellationToken);
}