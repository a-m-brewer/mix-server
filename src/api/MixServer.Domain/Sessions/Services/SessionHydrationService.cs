using MixServer.Domain.FileExplorer.Services;
using MixServer.Domain.Sessions.Entities;
using MixServer.Domain.Users.Services;

namespace MixServer.Domain.Sessions.Services;

public interface ISessionHydrationService
{
    Task HydrateAsync(IPlaybackSession session);
}

public class SessionHydrationService(
    IFileService fileService,
    IPlaybackTrackingService playbackTrackingService,
    IStreamKeyService streamKeyService) : ISessionHydrationService
{
    public async Task HydrateAsync(IPlaybackSession session)
    {
        var currentNode = await fileService.GetFileAsync(session.AbsolutePath);

        playbackTrackingService.Populate(session);
        
        session.StreamKey = streamKeyService.GenerateKey(session.Id.ToString());

        session.File = currentNode;
    }
}