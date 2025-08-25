using MixServer.Domain.FileExplorer.Entities;

namespace MixServer.Domain.Queueing.Entities;

public class QueueItemEntity
{
    public required Guid Id { get; init; }
    
    public FileExplorerFileNodeEntity? File { get; init; }
    public required Guid? FileId { get; init; }
    
    public required string Rank { get; set; }
    
    public Guid QueueId { get; init; }
    public required QueueEntity Queue { get; init; }
}