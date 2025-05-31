using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;
using MixServer.Domain.Persistence;

namespace MixServer.Domain.Interfaces;

public class RequestDto<T>(T request, Action onComplete) : IDisposable
    where T : notnull
{
    public T Request { get; } = request;

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        
        onComplete.Invoke();
    }
}

public interface IChannel<T> : ISingletonRepository where T : notnull
{
    Task WriteAsync(T request, CancellationToken cancellationToken = default);
    void Write(T request);
    Task<bool> WaitToReadAsync(CancellationToken stoppingToken);
    bool TryRead([MaybeNullWhen(false)] out RequestDto<T> request);
    void Complete();
}

public class ChannelBase<T>(
    bool deDuplicateRequests = true,
    bool singleReader = false,
    bool singleWriter = false) : IChannel<T>
    where T : notnull
{
    private readonly ConcurrentDictionary<T, byte>? _inFlight = deDuplicateRequests ? new ConcurrentDictionary<T, byte>() : null;
    private readonly Channel<T> _channel = Channel.CreateUnbounded<T>(
        new UnboundedChannelOptions
        {
            SingleReader = singleReader,
            SingleWriter = singleWriter,
            AllowSynchronousContinuations = false
        });

    public async Task WriteAsync(T request, CancellationToken cancellationToken = default)
    {
        if (_inFlight is null || _inFlight.TryAdd(request, 0))
        {
            await _channel.Writer.WriteAsync(request, cancellationToken);
        }
    }
    
    public void Write(T request)
    {
        if (_inFlight is null || _inFlight.TryAdd(request, 0))
        {
            _channel.Writer.TryWrite(request);
        }
    }

    public async Task<bool> WaitToReadAsync(CancellationToken stoppingToken)
    {
        return await _channel.Reader.WaitToReadAsync(stoppingToken);
    }

    public bool TryRead([MaybeNullWhen(false)] out RequestDto<T> request)
    {
        if (_channel.Reader.TryRead(out var requestValue))
        {
            request = new RequestDto<T>(requestValue, () =>
            {
                _inFlight?.TryRemove(requestValue, out _);
            });
            return true;
        }
        
        request = null;
        return false;
    }

    public void Complete()
    {
        _channel.Writer.Complete();
    }
}