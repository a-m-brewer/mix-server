using MixServer.Application.FileExplorer.Queries.GetNode;
using MixServer.Application.Queueing.Responses;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Queueing.Entities;
using MixServer.Domain.Queueing.Models;

namespace MixServer.Application.Queueing.Converters;

public interface IQueueDtoConverter :
    IConverter<QueueItemEntity, QueueSnapshotItemDto>,
    IConverter<Page, List<QueueItemEntity>, QueuePageDto>,
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

    public QueuePageDto Convert(Page page, List<QueueItemEntity> value)
    {
        return new QueuePageDto
        {
            Items = value.Select(Convert)
                .ToList(),
            PageIndex = page.PageIndex
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