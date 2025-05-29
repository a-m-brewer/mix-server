using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.Persistence;
using MixServer.Domain.Streams.Entities;

namespace MixServer.Domain.Streams.Repositories;

public interface ITranscodeRepository : ITransientRepository
{
    Task<Transcode> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<Transcode?> GetOrDefaultAsync(NodePath path, CancellationToken cancellationToken);
    Task AddAsync(Transcode transcode, CancellationToken cancellationToken);
    void Remove(Guid transcodeId);
}