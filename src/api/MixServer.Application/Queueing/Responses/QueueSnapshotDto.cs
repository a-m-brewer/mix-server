namespace MixServer.Application.Queueing.Responses;

public class QueueSnapshotDto
{
    public required List<QueueSnapshotItemDto> Items { get; init; }
}