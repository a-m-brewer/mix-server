using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.Persistence;
using MixServer.Domain.Streams.Entities;

namespace MixServer.Domain.Streams.Repositories;

public interface ITranscodeRepository : ITransientRepository
{
    Task<Transcode> GetAsync(Guid id);
    Task<Transcode> GetAsync(NodePath path);
    Task<Transcode?> GetOrDefaultAsync(NodePath path);
    Task<Transcode> GetOrAddAsync(NodePath path);
}