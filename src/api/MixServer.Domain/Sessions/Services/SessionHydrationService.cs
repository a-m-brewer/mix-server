using MixServer.Domain.FileExplorer.Services;
using MixServer.Domain.Sessions.Entities;

namespace MixServer.Domain.Sessions.Services;

public interface ISessionHydrationService
{
    void Hydrate(IPlaybackSession session);
}

public class SessionHydrationService(
    IFileService fileService,
    IPlaybackTrackingService playbackTrackingService) : ISessionHydrationService
{
    public void Hydrate(IPlaybackSession session)
    {
        var currentNode = fileService.GetFile(session.AbsolutePath);

        playbackTrackingService.Populate(session);

        session.File = currentNode;
    }
}