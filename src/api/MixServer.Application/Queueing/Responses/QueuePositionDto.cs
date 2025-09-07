namespace MixServer.Application.Queueing.Responses;

public class QueuePositionDto
{
    public required QueueSnapshotItemDto? CurrentQueuePosition { get; set; }
    
    public required QueueSnapshotItemDto? PreviousQueuePosition { get; init; }

    public required QueueSnapshotItemDto? NextQueuePosition { get; init; }
}