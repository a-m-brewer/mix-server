namespace MixServer.Domain.Queueing.Entities;

public class QueuePosition
{
    public required Guid? PreviousQueuePosition { get; init; }
    
    public required Guid? CurrentQueuePosition { get; init; }
    
    public required Guid? NextQueuePosition { get; init; }
}