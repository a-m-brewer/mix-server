using MixServer.Application.FileExplorer.Queries.GetNode;
using MixServer.Domain.Queueing.Enums;

namespace MixServer.Application.Queueing.Responses;

public class QueueSnapshotItemDto
{
    public required Guid Id { get; init; }
    public required QueueSnapshotItemType Type { get; init; }
    public required FileExplorerFileNodeResponse File { get; init; }
}