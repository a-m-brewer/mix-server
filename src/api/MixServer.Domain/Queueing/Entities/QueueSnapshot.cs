namespace MixServer.Domain.Queueing.Entities;

public class QueueSnapshot : IEquatable<QueueSnapshot>
{
    public QueueSnapshot(Guid? currentQueuePosition, List<QueueSnapshotItem> items)
    {
        CurrentQueuePosition = currentQueuePosition;
        Items = items;
    }

    public Guid? CurrentQueuePosition { get; }

    public QueueSnapshotItem? CurrentQueuePositionItem => CurrentQueuePosition.HasValue
        ? Items.FirstOrDefault(f => f.Id == CurrentQueuePosition)
        : null;

    public List<QueueSnapshotItem> Items { get; }

    public static QueueSnapshot Empty => new(null, []);

    public bool Equals(QueueSnapshot? other)
    {
        if (other == null)
        {
            return false;
        }

        if (Items.Count != other.Items.Count)
        {
            return false;
        }
        
        for (var i = 0; i < Items.Count; i++)
        {
            if (Items[i].Id != other.Items[i].Id)
            {
                return false;
            }
        }

        return true;
    }
}