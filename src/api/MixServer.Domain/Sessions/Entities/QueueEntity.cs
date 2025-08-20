using MixServer.Domain.FileExplorer.Entities;

namespace MixServer.Domain.Sessions.Entities;

public class QueueEntity
{
    public required Guid Id { get; init; }
    
    public required string UserId { get; init; }
    
    public FileExplorerFileNodeEntity? CurrentPosition { get; set; }
    
    public Guid? CurrentPositionId { get; set; }
    
    public FileExplorerFolderNodeEntity? CurrentFolder { get; set; }
    
    public Guid? CurrentFolderId { get; set; }
    
    public List<UserQueueItem> UserQueueItems { get; set; } = [];

    public void SetCurrentFolderAndPosition(PlaybackSession? nextSession)
    {
        CurrentFolder = nextSession?.Node?.Parent;
        CurrentFolderId = CurrentFolder?.Id;

        CurrentPosition = nextSession?.Node;
        CurrentPositionId = CurrentPosition?.Id;
    }
}