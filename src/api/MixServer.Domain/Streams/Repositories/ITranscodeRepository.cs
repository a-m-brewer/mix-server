using MixServer.Domain.Persistence;
using MixServer.Domain.Streams.Entities;

namespace MixServer.Domain.Streams.Repositories;

public interface ITranscodeRepository : ITransientRepository
{
    Task<Transcode> GetAsync(Guid id);
    Task<Transcode> GetAsync(string fileAbsolutePath);
    Task<Transcode?> GetOrDefaultAsync(string fileAbsolutePath);
    Task<Transcode> GetOrAddAsync(string fileAbsolutePath);
}