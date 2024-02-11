using MixServer.Application.FileExplorer.Queries.GetNode;
using MixServer.Domain.Queueing.Enums;

namespace MixServer.Application.Queueing.Responses;

public class QueueSnapshotItemDto
{
    public Guid Id { get; set; }

    public QueueSnapshotItemType Type { get; set; }
    public FileNodeResponse File { get; set; } = null!;
}