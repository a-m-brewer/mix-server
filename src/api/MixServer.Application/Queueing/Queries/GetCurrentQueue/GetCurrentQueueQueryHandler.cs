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
    IPageConverter pageConverter,
    IUnitOfWork unitOfWork)
    : IQueryHandler<GetCurrentQueueRequest, QueuePageDto>
{
    public async Task<QueuePageDto> HandleAsync(GetCurrentQueueRequest request, CancellationToken cancellationToken = default)
    {
        var page = pageConverter.Convert(request.Page);

        var queue = await userQueueService.GetQueuePageAsync(page, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var currentPosition = await userQueueService.GetCurrentPositionAsync(cancellationToken);
        
        return queueConverter.Convert(page, queue, currentPosition);
    }
}