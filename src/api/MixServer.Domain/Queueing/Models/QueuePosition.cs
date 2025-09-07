using MixServer.Domain.Queueing.Entities;

namespace MixServer.Domain.Queueing.Models;

public record QueuePosition(QueueItemEntity? Current, QueueItemEntity? Next, QueueItemEntity? Previous)
{
    public static QueuePosition Empty => new(null, null, null);
}