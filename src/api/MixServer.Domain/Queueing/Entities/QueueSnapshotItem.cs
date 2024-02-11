using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.Queueing.Enums;

namespace MixServer.Domain.Queueing.Entities;

public class QueueSnapshotItem
{
    public QueueSnapshotItem(Guid id, QueueSnapshotItemType type, IFileExplorerFileNode file)
    {
        Id = id;
        Type = type;
        File = file;
    }

    public Guid Id { get; }

    public QueueSnapshotItemType Type { get; }

    public IFileExplorerFileNode File { get; }
}