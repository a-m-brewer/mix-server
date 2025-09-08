namespace MixServer.Application.Queueing.Responses;

public class QueuePageDto
{
    public required int PageIndex { get; init; }
    public required List<QueueSnapshotItemDto> Items { get; init; }
}