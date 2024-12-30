using MixServer.Domain.FileExplorer.Services;
using MixServer.Domain.Sessions.Entities;
using MixServer.Domain.Tracklists.Services;

namespace MixServer.Domain.Sessions.Services;

public interface ISessionHydrationService
{
    void Hydrate(IPlaybackSession session);
}

public class SessionHydrationService(
    IFileService fileService,
    IPlaybackTrackingService playbackTrackingService,
    ITracklistTagService tracklistTagService) : ISessionHydrationService
{
    public void Hydrate(IPlaybackSession session)
    {
        var currentNode = fileService.GetFile(session.AbsolutePath);

        playbackTrackingService.Populate(session);

        session.File = currentNode;
        session.Tracklist = tracklistTagService.GetTracklistForFile(session.AbsolutePath);
    }
}