using MixServer.Domain.Queueing.Services;
using MixServer.Domain.Sessions.Services;
using MixServer.Infrastructure.Users.Repository;

namespace MixServer.Middleware;

public class CurrentUserMiddleware
{
    private readonly ILogger<CurrentUserMiddleware> _logger;
    private readonly RequestDelegate _next;

    public CurrentUserMiddleware(
        ILogger<CurrentUserMiddleware> logger,
        RequestDelegate next)
    {
        _logger = logger;
        _next = next;
    }
    
    public async Task InvokeAsync(
        HttpContext context,
        ICurrentUserRepository currentUserRepository,
        ISessionService sessionService,
        IQueueService queueService)
    {
        await currentUserRepository.LoadUserAsync();
        
        if (currentUserRepository.CurrentUserLoaded)
        {
            await sessionService.LoadPlaybackStateAsync();
            await queueService.LoadQueueStateAsync();
        }
        else
        {
            _logger.LogWarning("Can not load state no user currently loaded");
        }

        await _next(context);
    }
}