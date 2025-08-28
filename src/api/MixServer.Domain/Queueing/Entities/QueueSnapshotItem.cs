using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.Queueing.Enums;

namespace MixServer.Domain.Queueing.Entities;

public class QueueSnapshotItem(Guid id, QueueItemType type, IFileExplorerFileNode file)
{
    public Guid Id { get; } = id;

    public QueueItemType Type { get; } = type;

    public IFileExplorerFileNode File { get; } = file;
}