using System.Threading.RateLimiting;
using MixServer.Domain.Utilities;

namespace MixServer.Infrastructure.Sessions.Services;

public interface IRateLimiter
{
}

public interface IRateLimiter<in TKey> : IRateLimiter where TKey : notnull
{
    bool TryAcquire(TKey id);
}

public abstract class RateLimiterBase<TKey> : IRateLimiter<TKey> where TKey : notnull
{
    private readonly IReadWriteLock _readWriteLock;
    private readonly Dictionary<TKey, RateLimiter> _rateLimiters = new();

    protected RateLimiterBase(IReadWriteLock readWriteLock)
    {
        _readWriteLock = readWriteLock;
    }
    
    public bool TryAcquire(TKey sessionId)
    {
        return _readWriteLock.ForUpgradeableRead(() =>
        {
            if (!_rateLimiters.TryGetValue(sessionId, out var limiter))
            {
                _readWriteLock.ForWrite(() =>
                {
                    limiter = CreateRateLimiter();
                    _rateLimiters.Add(sessionId, limiter);
                });
            }

            var lease = limiter!.AttemptAcquire();

            return lease.IsAcquired;
        });
    }
    
    protected abstract TimeSpan Window { get; }

    private RateLimiter CreateRateLimiter()
    {
        return new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
        {
            QueueLimit = 1,
            AutoReplenishment = true,
            PermitLimit = 1,
            Window = Window,
            QueueProcessingOrder = QueueProcessingOrder.NewestFirst
        });
    }
}