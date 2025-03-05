using MixServer.Domain.Users.Models;

namespace MixServer.Domain.Queueing.Entities;

public class QueueSnapshot(
    Guid? currentQueuePosition,
    Guid? previousQueuePosition,
    Guid? nextQueuePosition,
    List<QueueSnapshotItem> items) : IEquatable<QueueSnapshot>
{
    public Guid? CurrentQueuePosition { get; } = currentQueuePosition;
    public Guid? PreviousQueuePosition { get; } = previousQueuePosition;
    public Guid? NextQueuePosition { get; } = nextQueuePosition;

    public QueueSnapshotItem? CurrentQueuePositionItem => CurrentQueuePosition.HasValue
        ? Items.FirstOrDefault(f => f.Id == CurrentQueuePosition)
        : null;

    public List<QueueSnapshotItem> Items { get; } = items;

    public static QueueSnapshot Empty => new(null, null, null, []);

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