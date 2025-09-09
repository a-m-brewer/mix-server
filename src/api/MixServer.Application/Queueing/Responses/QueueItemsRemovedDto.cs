namespace MixServer.Application.Queueing.Responses;

public class QueueItemsRemovedDto
{
    public required List<Guid> RemovedItemIds { get; init; }
    
    public required QueuePositionDto CurrentPosition { get; init; }
}