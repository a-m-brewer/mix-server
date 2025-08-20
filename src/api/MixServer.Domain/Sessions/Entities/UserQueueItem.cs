using MixServer.Domain.FileExplorer.Entities;

namespace MixServer.Domain.Sessions.Entities;

public class UserQueueItem
{
    public required Guid Id { get; init; }
    
    public required FileExplorerFileNodeEntity File { get; init; }
    public required Guid FileId { get; init; }
    
    public required QueueEntity Queue { get; init; }
    public required Guid QueueId { get; init; }
    
    public required Guid? PreviousFolderItemId { get; init; }
    
    public FileExplorerFileNodeEntity? PreviousFolderItem { get; init; }
    
    public required DateTime AddedToQueue { get; init; }
}