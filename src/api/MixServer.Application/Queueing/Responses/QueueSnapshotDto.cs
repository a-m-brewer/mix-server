namespace MixServer.Application.Queueing.Responses;

public class QueueSnapshotDto
{
    public Guid? CurrentQueuePosition { get; set; }
    public List<QueueSnapshotItemDto> Items { get; set; } = [];
}