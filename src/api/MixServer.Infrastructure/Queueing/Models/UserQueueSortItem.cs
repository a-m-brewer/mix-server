namespace MixServer.Infrastructure.Queueing.Models;

public class UserQueueSortItem : QueueSortItem
{
    public UserQueueSortItem(Guid id, string absoluteFilePath, Guid? previousFolderItemId) : base(id, absoluteFilePath)
    {
        PreviousFolderItemId = previousFolderItemId;
    }

    public Guid? PreviousFolderItemId { get; }

    public DateTime Added { get; set; } = DateTime.UtcNow;
}