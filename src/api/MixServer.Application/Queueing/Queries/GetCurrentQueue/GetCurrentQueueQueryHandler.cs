using MixServer.Application.Queueing.Responses;
using MixServer.Domain.Queueing.Entities;
using MixServer.Domain.Queueing.Services;
using MixServer.Shared.Interfaces;

namespace MixServer.Application.Queueing.Queries.GetCurrentQueue;

public class GetCurrentQueueQueryHandler(
    IConverter<QueueSnapshot, QueueSnapshotDto> queueSnapshotDtoConverter,
    IQueueService queueService)
    : IQueryHandler<QueueSnapshotDto>
{
    public async Task<QueueSnapshotDto> HandleAsync()
    {
        var queue = await queueService.GenerateQueueSnapshotAsync();

        return queueSnapshotDtoConverter.Convert(queue);
    }
}