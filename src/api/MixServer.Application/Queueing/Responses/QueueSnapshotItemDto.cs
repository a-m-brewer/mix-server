using MixServer.Application.FileExplorer.Queries.GetNode;
using MixServer.Domain.Queueing.Enums;

namespace MixServer.Application.Queueing.Responses;

public class QueueSnapshotItemDto
{
    public required Guid Id { get; init; }
    
    public required QueueItemType Type { get; init; }
    
    public required string Rank { get; init; }
    
    public required bool IsCurrentPosition { get; init; }
    
    public required FileExplorerFileNodeResponse File { get; init; }
}