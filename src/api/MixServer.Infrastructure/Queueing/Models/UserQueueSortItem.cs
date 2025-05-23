namespace MixServer.Infrastructure.Queueing.Models;

public class UserQueueSortItem : QueueSortItem
{
    public required Guid? PreviousFolderItemId { get; init; }

    public DateTime Added { get; } = DateTime.UtcNow;
}