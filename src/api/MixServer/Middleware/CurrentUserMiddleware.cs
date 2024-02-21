using MixServer.Domain.Queueing.Services;
using MixServer.Domain.Sessions.Services;
using MixServer.Infrastructure.Users.Repository;

namespace MixServer.Middleware;

public class CurrentUserMiddleware(
    ILogger<CurrentUserMiddleware> logger,
    RequestDelegate next)
{
    public async Task InvokeAsync(
        HttpContext context,
        ICurrentUserRepository currentUserRepository,
        ISessionService sessionService,
        IQueueService queueService)
    {
        await currentUserRepository.LoadUserAsync();
        
        if (currentUserRepository.CurrentUserLoaded)
        {
            try
            {
                await sessionService.LoadPlaybackStateAsync();
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to Load Playback State");
            }
            
            
            try
            {
                await queueService.LoadQueueStateAsync();
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to Load Queue State");
            }
        }
        else
        {
            logger.LogWarning("Can not load state no user currently loaded");
        }

        await next(context);
    }
}