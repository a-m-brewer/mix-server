using MixServer.Application.FileExplorer.Dtos;

namespace MixServer.Application.Queueing.Queries.GetCurrentQueue;

public class GetCurrentQueueRequest
{
    public required RangeDto Range { get; init; }
}