namespace MixServer.Application.Queueing.Responses;

public class QueueSnapshotDto
{
    public required Guid? CurrentQueuePosition { get; set; }
    
    public required Guid? PreviousQueuePosition { get; init; }

    public required Guid? NextQueuePosition { get; init; }

    public required List<QueueSnapshotItemDto> Items { get; init; }
}