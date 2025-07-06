using MixServer.Domain.Sessions.Entities;
using MixServer.Domain.Users.Services;

namespace MixServer.Domain.Sessions.Services;

public interface ISessionHydrationService
{
    Task HydrateAsync(IPlaybackSession session);
}

public class SessionHydrationService(
    IPlaybackTrackingService playbackTrackingService,
    IStreamKeyService streamKeyService) : ISessionHydrationService
{
    public Task HydrateAsync(IPlaybackSession session)
    {
        playbackTrackingService.Populate(session);
        
        session.StreamKey = streamKeyService.GenerateKey(session.Id.ToString());
        
        return Task.CompletedTask;
    }
}