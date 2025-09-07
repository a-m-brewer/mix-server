using MixServer.Application.FileExplorer.Queries.GetNode;
using MixServer.Application.Queueing.Responses;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Queueing.Entities;
using MixServer.Domain.Queueing.Models;

namespace MixServer.Application.Queueing.Converters;

public interface IQueueDtoConverter :
    IConverter<QueueItemEntity, QueueSnapshotItemDto>,
    IConverter<List<QueueItemEntity>, QueueSnapshotDto>,
    IConverter<QueuePosition, QueuePositionDto>;

public class QueueDtoConverter(
    IFileExplorerEntityToResponseConverter fileNodeResponseConverter)
    : IQueueDtoConverter
{
    public QueueSnapshotItemDto Convert(QueueItemEntity value)
    {
        return new QueueSnapshotItemDto
        {
            Id = value.Id,
            Type = value.Type,
            File = fileNodeResponseConverter.Convert(value.File ??
                                                     throw new InvalidOperationException(
                                                         "Queue item must have a file."))
        };
    }

    public QueueSnapshotDto Convert(List<QueueItemEntity> value)
    {
        return new QueueSnapshotDto
        {
            Items = value.Select(Convert).ToList()
        };
    }

    public QueuePositionDto Convert(QueuePosition value)
    {
        return new QueuePositionDto
        {
            CurrentQueuePosition = value.Current is null ? null : Convert(value.Current),
            PreviousQueuePosition = value.Previous is null ? null : Convert(value.Previous),
            NextQueuePosition = value.Next is null ? null : Convert(value.Next)
        };
    }
}