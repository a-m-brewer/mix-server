using MixServer.Domain.FileExplorer.Models;

namespace MixServer.Infrastructure.Queueing.Models;

public abstract class QueueSortItem
{
    public required Guid Id { get; init; }

    public required NodePath NodePath { get; init; }
}