using System.Collections.Concurrent;
using DebounceThrottle;
using Microsoft.Extensions.Logging;

namespace MixServer.Infrastructure.Sessions.Services;

public class KeyedDebouncer<TKey>(TimeSpan interval, ILogger logger, TimeSpan? maxDelay = null)
    where TKey : notnull
{
    private readonly ConcurrentDictionary<TKey, DebounceDispatcher> _rateLimiters = new();

    public async Task DebounceAsync(TKey key, Func<Task> action)
    {
        if (!_rateLimiters.TryGetValue(key, out var dispatcher))
        {
            dispatcher = new DebounceDispatcher(interval, maxDelay);
            _rateLimiters[key] = dispatcher;
        }

        await dispatcher.DebounceAsync(async void () =>
        {
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during debounced action for key {Key}", key);
            }
        });
    }
}