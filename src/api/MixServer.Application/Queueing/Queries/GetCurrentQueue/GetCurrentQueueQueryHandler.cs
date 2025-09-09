using MixServer.Application.FileExplorer.Converters;
using MixServer.Application.Queueing.Converters;
using MixServer.Application.Queueing.Responses;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Persistence;
using MixServer.Domain.Queueing.Services;

namespace MixServer.Application.Queueing.Queries.GetCurrentQueue;

public class GetCurrentQueueQueryHandler(
    IQueueDtoConverter queueConverter,
    IUserQueueService userQueueService,
    IRangeConverter rangeConverter,
    IUnitOfWork unitOfWork)
    : IQueryHandler<GetCurrentQueueRequest, QueueRangeDto>
{
    public async Task<QueueRangeDto> HandleAsync(GetCurrentQueueRequest request, CancellationToken cancellationToken = default)
    {
        var range = rangeConverter.Convert(request.Range);
        
        var queue = await userQueueService.GetQueueRangeAsync(range, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var currentPosition = await userQueueService.GetCurrentPositionAsync(cancellationToken);
        
        return queueConverter.Convert(queue, currentPosition);
    }
}