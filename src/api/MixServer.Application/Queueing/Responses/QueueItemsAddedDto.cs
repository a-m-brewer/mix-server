namespace MixServer.Application.Queueing.Responses;

public class QueueItemsAddedDto
{
    public required List<QueueSnapshotItemDto> AddedItems { get; init; }
    
    public required QueuePositionDto CurrentPosition { get; init; }
}