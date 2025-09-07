using System.ComponentModel.DataAnnotations.Schema;
using MixServer.Domain.FileExplorer.Entities;

namespace MixServer.Domain.Queueing.Entities;

public class QueueEntity
{
    public required Guid Id { get; init; }
    
    public required string UserId { get; init; }
    
    public QueueItemEntity? CurrentPosition { get; set; }
    public Guid? CurrentPositionId { get; set; }
    
    public FileExplorerFolderNodeEntity? CurrentFolder { get; set; }
    public Guid? CurrentFolderId { get; set; }

    public FileExplorerFolderNodeEntity? CurrentRootChild { get; set; }
    public Guid? CurrentRootChildId { get; set; }

    [NotMapped]
    public IFileExplorerFolderEntity? CurrentFolderEntity => CurrentFolder ?? CurrentRootChild;

    public List<QueueItemEntity> Items { get; init; } = [];
}