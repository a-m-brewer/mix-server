using MixServer.Application.Queueing.Responses;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Queueing.Entities;
using MixServer.Domain.Queueing.Services;

namespace MixServer.Application.Queueing.Queries.GetCurrentQueue;

public class GetCurrentQueueQueryHandler(
    IConverter<QueueSnapshot, QueueSnapshotDto> queueSnapshotDtoConverter,
    IConverter<QueueSnapshotItem, QueueSnapshotItemDto> queueSnapshotItemDtoConverter,
    IQueueService queueService)
    : IQueryHandler<GetCurrentQueueQuery, QueueSnapshotDto>
{
    public async Task<QueueSnapshotDto> HandleAsync(GetCurrentQueueQuery query, CancellationToken cancellationToken = default)
    {
        // If pagination parameters are provided, use range-based generation
        if (query.StartIndex.HasValue && query.EndIndex.HasValue)
        {
            var (snapshot, totalCount) = await queueService.GenerateQueueSnapshotRangeAsync(
                query.StartIndex.Value,
                query.EndIndex.Value,
                cancellationToken);

            return new QueueSnapshotDto
            {
                CurrentQueuePosition = snapshot.CurrentQueuePosition,
                PreviousQueuePosition = snapshot.PreviousQueuePosition,
                NextQueuePosition = snapshot.NextQueuePosition,
                Items = snapshot.Items
                    .Select(s => queueSnapshotItemDtoConverter.Convert(s))
                    .ToList(),
                TotalCount = totalCount
            };
        }

        // Otherwise, return the full queue (backward compatibility)
        var fullQueue = await queueService.GenerateQueueSnapshotAsync(cancellationToken);
        var dto = queueSnapshotDtoConverter.Convert(fullQueue);
        dto.TotalCount = fullQueue.Items.Count;
        return dto;
    }
}
