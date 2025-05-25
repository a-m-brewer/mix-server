using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.Persistence;
using MixServer.Domain.Streams.Entities;

namespace MixServer.Domain.Streams.Repositories;

public interface ITranscodeRepository : ITransientRepository
{
    Task<Transcode> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Transcode?> GetOrDefaultAsync(NodePath path);
    Task AddAsync(Transcode transcode);
    void Remove(Guid transcodeId);
}