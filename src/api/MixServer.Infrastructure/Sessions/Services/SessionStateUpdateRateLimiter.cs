using MixServer.Domain.Utilities;

namespace MixServer.Infrastructure.Sessions.Services;

public interface ISaveSessionStateRateLimiter : IRateLimiter<Guid>
{
}

public class SaveSessionStateRateLimiter(IReadWriteLock readWriteLock)
    : RateLimiterBase<Guid>(readWriteLock), ISaveSessionStateRateLimiter
{
    protected override TimeSpan Window => TimeSpan.FromSeconds(2);
}