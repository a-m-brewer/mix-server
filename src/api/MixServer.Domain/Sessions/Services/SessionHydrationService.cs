using MixServer.Domain.FileExplorer.Models;
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
    IStreamKeyService streamKeyService,
    IRootFileExplorerFolder rootFolder) : ISessionHydrationService
{
    public void Hydrate(IPlaybackSession session)
    {
        var nodePath = rootFolder.GetNodePath(session.AbsolutePath);
        
        var currentNode = fileService.GetFile(nodePath);

        playbackTrackingService.Populate(session);
        
        session.StreamKey = streamKeyService.GenerateKey(session.Id.ToString());

        session.File = currentNode;
    }
}