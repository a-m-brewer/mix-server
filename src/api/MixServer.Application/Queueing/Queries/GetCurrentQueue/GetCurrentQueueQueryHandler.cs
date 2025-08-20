using MixServer.Application.Queueing.Responses;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Persistence;
using MixServer.Domain.Queueing.Entities;
using MixServer.Domain.Queueing.Services;

namespace MixServer.Application.Queueing.Queries.GetCurrentQueue;

public class GetCurrentQueueQueryHandler(
    IConverter<QueueSnapshot, QueueSnapshotDto> queueSnapshotDtoConverter,
    IQueueService queueService,
    IUnitOfWork unitOfWork)
    : IQueryHandler<GetCurrentQueueRequest, QueueSnapshotDto>
{
    public async Task<QueueSnapshotDto> HandleAsync(GetCurrentQueueRequest request, CancellationToken cancellationToken = default)
    {
        var queue = await queueService.GenerateQueueSnapshotAsync(cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        
        return queueSnapshotDtoConverter.Convert(queue);
    }
}