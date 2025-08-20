namespace MixServer.Application.Queueing.Responses;

public class QueuePositionDto
{
    public required Guid? CurrentQueuePosition { get; set; }
    
    public required Guid? PreviousQueuePosition { get; init; }

    public required Guid? NextQueuePosition { get; init; }
}