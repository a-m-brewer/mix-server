namespace MixServer.Infrastructure.Queueing.Models;

public class FolderQueueSortItem(Guid id, string absoluteFilePath, int position) : QueueSortItem(id, absoluteFilePath)
{
    public int Position { get; set; } = position;
}