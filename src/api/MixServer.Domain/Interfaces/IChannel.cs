using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using MixServer.Domain.Persistence;
using MixServer.Domain.Utilities;

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
    event EventHandler RequestsChanged;
    
    IReadOnlyCollection<T> Requests { get; }
    
    Task WriteAsync(T request, CancellationToken cancellationToken = default);
    void Write(T request);
    Task<bool> WaitToReadAsync(CancellationToken stoppingToken);
    bool TryRead([MaybeNullWhen(false)] out RequestDto<T> request);
    void Complete();
}

public class ChannelBase<T> : IChannel<T>
    where T : IChannelMessage
{
    private readonly ReadWriteLock _requestsLock = new();
    private readonly ObservableCollection<T> _requests;
    
    private readonly ConcurrentDictionary<T, byte>? _inFlight;
    private readonly Channel<T> _channel;

    private readonly ILogger _logger;

    public ChannelBase(ILogger logger,
        bool deDuplicateRequests = true,
        bool singleReader = false,
        bool singleWriter = false)
    {
        _logger = logger;
        _inFlight = deDuplicateRequests ? new ConcurrentDictionary<T, byte>() : null;
        _requests = new ObservableCollection<T>();
        _requests.CollectionChanged += RequestsOnCollectionChanged;
        _channel = Channel.CreateUnbounded<T>(
            new UnboundedChannelOptions
            {
                SingleReader = singleReader,
                SingleWriter = singleWriter,
                AllowSynchronousContinuations = false
            });
    }

    public event EventHandler? RequestsChanged;

    public IReadOnlyCollection<T> Requests => _requestsLock.ForRead(() => _requests.ToImmutableList());

    public async Task WriteAsync(T request, CancellationToken cancellationToken = default)
    {
        if (_inFlight is null || _inFlight.TryAdd(request, 0))
        {
            _logger.LogTrace("Writing request {RequestIdentifier} to channel: {ChannelType}", request.Identifier, GetType().Name);
            await _channel.Writer.WriteAsync(request, cancellationToken);
            _requestsLock.ForWrite(() => _requests.Add(request));
            _logger.LogDebug("Request {RequestIdentifier} written to channel: {ChannelType}", request.Identifier, GetType().Name);
        }
        else
        {
            _logger.LogWarning("Request {RequestIdentifier} is already in flight on channel: {ChannelType} and will not be written again.", request.Identifier, GetType().Name);
        }
    }
    
    public void Write(T request)
    {
        if (_inFlight is null || _inFlight.TryAdd(request, 0))
        {
            _logger.LogTrace("Writing request {RequestIdentifier} to channel: {ChannelType}", request.Identifier, GetType().Name);
            if (_channel.Writer.TryWrite(request))
            {
                _requestsLock.ForWrite(() => _requests.Add(request));
            }
            _logger.LogDebug("Request {RequestIdentifier} written to channel: {ChannelType}", request.Identifier, GetType().Name);
        }
        else
        {
            _logger.LogWarning("Request {RequestIdentifier} is already in flight on channel: {ChannelType} and will not be written again.", request.Identifier, GetType().Name);
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
                _requestsLock.ForWrite(() => _requests.Remove(requestValue));
                _inFlight?.TryRemove(requestValue, out _);
                _logger.LogTrace("Request {RequestIdentifier} removed from in-flight on channel: {ChannelType}", requestValue.Identifier, GetType().Name);
            });
            return true;
        }
        
        request = null;
        return false;
    }

    public void Complete()
    {
        _channel.Writer.Complete();
        _logger.LogInformation("Channel {ChannelType} completed.", GetType().Name);
    }
    
    private void RequestsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        _ = Task.Run(() => RequestsChanged?.Invoke(this, EventArgs.Empty));
    }
}