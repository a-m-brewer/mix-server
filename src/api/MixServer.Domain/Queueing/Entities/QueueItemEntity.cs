using MixServer.Domain.FileExplorer.Entities;
using MixServer.Domain.Queueing.Enums;

namespace MixServer.Domain.Queueing.Entities;

public class QueueItemEntity
{
    public required Guid Id { get; init; }
    
    public FileExplorerFileNodeEntity? File { get; init; }
    public required Guid? FileId { get; init; }
    
    public required string Rank { get; set; }
    
    public QueueItemType Type { get; init; }
    
    public Guid QueueId { get; init; }
    public required QueueEntity Queue { get; init; }
}