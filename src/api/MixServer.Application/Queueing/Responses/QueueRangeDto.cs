namespace MixServer.Application.Queueing.Responses;

public class QueueRangeDto
{
    public required List<QueueSnapshotItemDto> Items { get; init; }
}