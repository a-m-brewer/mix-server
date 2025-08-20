using MixServer.Application.FileExplorer.Dtos;

namespace MixServer.Application.Queueing.Queries.GetCurrentQueue;

public class GetCurrentQueueRequest
{
    public required PageDto Page { get; init; }
}