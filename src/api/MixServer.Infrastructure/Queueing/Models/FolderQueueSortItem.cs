namespace MixServer.Infrastructure.Queueing.Models;

public class FolderQueueSortItem : QueueSortItem
{
    public FolderQueueSortItem(Guid id, string absoluteFilePath, int position) : base(id, absoluteFilePath)
    {
        Position = position;
    }
    
    public int Position { get; set; }
}