using MixServer.Application.FileExplorer.Queries.GetNode;
using MixServer.Application.Queueing.Responses;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Queueing.Entities;

namespace MixServer.Application.Queueing.Converters;

public class QueueSnapshotDtoConverter(
    IConverter<IFileExplorerFileNode, FileExplorerFileNodeResponse> fileNodeResponseConverter)
    :
        IConverter<QueueSnapshot, QueueSnapshotDto>,
        IConverter<QueueSnapshotItem, QueueSnapshotItemDto>

{
    public QueueSnapshotDto Convert(QueueSnapshot value)
    {
        return new QueueSnapshotDto
        {
            CurrentQueuePosition = value.CurrentQueuePosition,
            Items = value.Items.Select(Convert).ToList()
        };
    }

    public QueueSnapshotItemDto Convert(QueueSnapshotItem value)
    {
        return new QueueSnapshotItemDto
        {
            Id = value.Id,
            Type = value.Type,
            File = fileNodeResponseConverter.Convert(value.File)
        };
    }
}