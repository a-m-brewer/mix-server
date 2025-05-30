using MixServer.Application.Queueing.Responses;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Queueing.Entities;
using MixServer.Domain.Queueing.Services;

namespace MixServer.Application.Queueing.Queries.GetCurrentQueue;

public class GetCurrentQueueQueryHandler(
    IConverter<QueueSnapshot, QueueSnapshotDto> queueSnapshotDtoConverter,
    IQueueService queueService)
    : IQueryHandler<QueueSnapshotDto>
{
    public async Task<QueueSnapshotDto> HandleAsync(CancellationToken cancellationToken = default)
    {
        var queue = await queueService.GenerateQueueSnapshotAsync(cancellationToken);

        return queueSnapshotDtoConverter.Convert(queue);
    }
}