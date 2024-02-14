namespace MixServer.Infrastructure.Queueing.Models;

public class UserQueueSortItem(Guid id, string absoluteFilePath, Guid? previousFolderItemId)
    : QueueSortItem(id, absoluteFilePath)
{
    public Guid? PreviousFolderItemId { get; } = previousFolderItemId;

    public DateTime Added { get; set; } = DateTime.UtcNow;
}