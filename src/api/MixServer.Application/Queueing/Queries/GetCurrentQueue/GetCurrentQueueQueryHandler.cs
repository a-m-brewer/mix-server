using MixServer.Application.Queueing.Responses;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Queueing.Entities;
using MixServer.Domain.Queueing.Services;

namespace MixServer.Application.Queueing.Queries.GetCurrentQueue;

public class GetCurrentQueueQueryHandler : IQueryHandler<QueueSnapshotDto>
{
    private readonly IConverter<QueueSnapshot, QueueSnapshotDto> _queueSnapshotDtoConverter;
    private readonly IQueueService _queueService;

    public GetCurrentQueueQueryHandler(
        IConverter<QueueSnapshot, QueueSnapshotDto> queueSnapshotDtoConverter,
        IQueueService queueService)
    {
        _queueSnapshotDtoConverter = queueSnapshotDtoConverter;
        _queueService = queueService;
    }

    public async Task<QueueSnapshotDto> HandleAsync()
    {
        var queue = await _queueService.GenerateQueueSnapshotAsync();

        return _queueSnapshotDtoConverter.Convert(queue);
    }
}