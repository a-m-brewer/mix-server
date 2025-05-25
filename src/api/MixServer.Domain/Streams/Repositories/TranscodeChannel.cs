using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;
using MixServer.Domain.Persistence;
using MixServer.Domain.Streams.Models;

namespace MixServer.Domain.Streams.Repositories;

public interface ITranscodeChannel : ISingletonRepository
{
    Task WriteAsync(TranscodeRequest request);
    Task<bool> WaitToReadAsync(CancellationToken stoppingToken);
    bool TryRead([MaybeNullWhen(false)] out TranscodeRequest request);
}

public class TranscodeChannel : ITranscodeChannel
{
    private readonly Channel<TranscodeRequest> _channel = Channel.CreateUnbounded<TranscodeRequest>(
        new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });

    public async Task WriteAsync(TranscodeRequest request)
    {
        await _channel.Writer.WriteAsync(request);
    }

    public async Task<bool> WaitToReadAsync(CancellationToken stoppingToken)
    {
        return await _channel.Reader.WaitToReadAsync(stoppingToken);
    }

    public bool TryRead([MaybeNullWhen(false)] out TranscodeRequest request)
    {
        return _channel.Reader.TryRead(out request);
    }
}