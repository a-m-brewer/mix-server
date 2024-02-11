using MixServer.Domain.Utilities;

namespace MixServer.Infrastructure.Sessions.Services;

public interface IUpdateSessionStateRateLimiter : IRateLimiter<string> {}

public class UpdateSessionStateRateLimiter : RateLimiterBase<string>, IUpdateSessionStateRateLimiter
{
    public UpdateSessionStateRateLimiter(IReadWriteLock readWriteLock) : base(readWriteLock)
    {
    }

    protected override TimeSpan Window => TimeSpan.FromSeconds(0.5);
}