using MixServer.Application.FileExplorer.Queries.GetNode;
using MixServer.Application.Queueing.Responses;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Queueing.Entities;
using MixServer.Domain.Queueing.Models;

namespace MixServer.Application.Queueing.Converters;

public interface IQueueDtoConverter :
    IConverter<QueueItemEntity, QueueItemEntity?, QueueSnapshotItemDto>,
    IConverter<List<QueueItemEntity>, QueueItemEntity?, QueueRangeDto>,
    IConverter<QueuePosition, QueuePositionDto>,
    IConverter<IEnumerable<QueueItemEntity>, QueuePosition, QueueItemsAddedDto>,
    IConverter<IEnumerable<Guid>, QueuePosition, QueueItemsRemovedDto>;

public class QueueDtoConverter(
    IFileExplorerEntityToResponseConverter fileNodeResponseConverter)
    : IQueueDtoConverter
{
    public QueueSnapshotItemDto Convert(QueueItemEntity value, QueueItemEntity? currentPosition)
    {
        return Convert(value, currentPosition != null && value.Id == currentPosition.Id);
    }
    
    private QueueSnapshotItemDto Convert(QueueItemEntity value, bool isCurrentPosition)
    {
        return new QueueSnapshotItemDto
        {
            Id = value.Id,
            Type = value.Type,
            File = fileNodeResponseConverter.Convert(value.File ??
                                                     throw new InvalidOperationException(
                                                         "Queue item must have a file.")),
            IsCurrentPosition = isCurrentPosition,
            Rank = value.Rank
        };
    }

    public QueueRangeDto Convert(List<QueueItemEntity> value, QueueItemEntity? currentPosition)
    {
        return new QueueRangeDto
        {
            Items = value.Select(s => Convert(s, currentPosition)).ToList()
        };
    }

    public QueuePositionDto Convert(QueuePosition value)
    {
        return new QueuePositionDto
        {
            CurrentQueuePosition = value.Current is null ? null : Convert(value.Current, true),
            PreviousQueuePosition = value.Previous is null ? null : Convert(value.Previous, false),
            NextQueuePosition = value.Next is null ? null : Convert(value.Next, false)
        };
    }

    public QueueItemsAddedDto Convert(IEnumerable<QueueItemEntity> value, QueuePosition value2)
    {
        return new QueueItemsAddedDto
        {
            AddedItems = value.Select(v => Convert(v, value2.Current != null && v.Id == value2.Current.Id)).ToList(),
            CurrentPosition = Convert(value2)
        };
    }

    public QueueItemsRemovedDto Convert(IEnumerable<Guid> value, QueuePosition value2)
    {
        return new QueueItemsRemovedDto
        {
            RemovedItemIds = value.ToList(),
            CurrentPosition = Convert(value2)
        };
    }
}