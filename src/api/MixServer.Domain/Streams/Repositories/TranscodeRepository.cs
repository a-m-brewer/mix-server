using System.Collections.Concurrent;
using MixServer.Domain.Exceptions;

namespace MixServer.Domain.Streams.Repositories;

public record TranscodeInfo(string AbsoluteFilePath, int? RequestedBitrate);

public interface ITranscodeRepository
{
    void AddTranscode(string hash, TranscodeInfo transcodeInfo);
    TranscodeInfo GetTranscode(string hash);
    void RemoveTranscode(string hash);
}

public class TranscodeRepository : ITranscodeRepository
{
    private readonly ConcurrentDictionary<string, TranscodeInfo> _transcodes = new();

    public void AddTranscode(string hash, TranscodeInfo transcodeInfo)
    {
        _transcodes[hash] = transcodeInfo;
    }

    public TranscodeInfo GetTranscode(string hash)
    {
        return _transcodes.TryGetValue(hash, out var transcodeInfo)
            ? transcodeInfo
            : throw new NotFoundException("TranscodeInfo", hash);
    }

    public void RemoveTranscode(string hash)
    {
        _transcodes.TryRemove(hash, out _);
    }
}