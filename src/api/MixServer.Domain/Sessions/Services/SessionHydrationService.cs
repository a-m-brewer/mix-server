using MixServer.Domain.FileExplorer.Services;
using MixServer.Domain.Sessions.Entities;
using MixServer.Domain.Users.Services;

namespace MixServer.Domain.Sessions.Services;

public interface ISessionHydrationService
{
    void Hydrate(IPlaybackSession session);
}

public class SessionHydrationService(
    IFileService fileService,
    IPlaybackTrackingService playbackTrackingService,
    IStreamKeyService streamKeyService) : ISessionHydrationService
{
    public void Hydrate(IPlaybackSession session)
    {
        var currentNode = fileService.GetFile(session.NodeEntity.Path);

        playbackTrackingService.Populate(session);
        
        session.StreamKey = streamKeyService.GenerateKey(session.Id.ToString());

        session.File = currentNode;
    }
}