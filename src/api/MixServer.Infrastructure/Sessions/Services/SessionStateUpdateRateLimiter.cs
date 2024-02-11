using MixServer.Domain.Utilities;

namespace MixServer.Infrastructure.Sessions.Services;

public interface ISaveSessionStateRateLimiter : IRateLimiter<Guid>
{
}

public class SaveSessionStateRateLimiter : RateLimiterBase<Guid>, ISaveSessionStateRateLimiter
{
    public SaveSessionStateRateLimiter(IReadWriteLock readWriteLock) : base(readWriteLock)
    {
    }

    protected override TimeSpan Window => TimeSpan.FromSeconds(2);
}