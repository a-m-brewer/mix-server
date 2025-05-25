using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;
using MixServer.Domain.Persistence;

namespace MixServer.Domain.Interfaces;

public interface IChannel<T> : ISingletonRepository
{
    Task WriteAsync(T request);
    Task<bool> WaitToReadAsync(CancellationToken stoppingToken);
    bool TryRead([MaybeNullWhen(false)] out T request);
}

public class ChannelBase<T> : IChannel<T>
{
    private readonly Channel<T> _channel = Channel.CreateUnbounded<T>(
        new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });

    public async Task WriteAsync(T request)
    {
        await _channel.Writer.WriteAsync(request);
    }

    public async Task<bool> WaitToReadAsync(CancellationToken stoppingToken)
    {
        return await _channel.Reader.WaitToReadAsync(stoppingToken);
    }

    public bool TryRead([MaybeNullWhen(false)] out T request)
    {
        return _channel.Reader.TryRead(out request);
    }
}