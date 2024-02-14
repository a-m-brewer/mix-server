using MixServer.Domain.Utilities;

namespace MixServer.Infrastructure.Sessions.Services;

public interface IUpdateSessionStateRateLimiter : IRateLimiter<string> {}

public class UpdateSessionStateRateLimiter(IReadWriteLock readWriteLock)
    : RateLimiterBase<string>(readWriteLock), IUpdateSessionStateRateLimiter
{
    protected override TimeSpan Window => TimeSpan.FromSeconds(0.5);
}