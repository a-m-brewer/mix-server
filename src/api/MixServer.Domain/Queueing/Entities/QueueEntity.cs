namespace MixServer.Domain.Queueing.Entities;

public class QueueEntity
{
    public required Guid Id { get; init; }
    
    public required string UserId { get; init; }
    
    public QueueItemEntity? CurrentPosition { get; set; }
    public Guid? CurrentPositionId { get; set; }

    public List<QueueItemEntity> Items { get; init; } = [];
}