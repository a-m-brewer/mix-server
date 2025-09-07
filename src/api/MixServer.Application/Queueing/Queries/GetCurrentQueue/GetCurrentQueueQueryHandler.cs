using MixServer.Application.FileExplorer.Converters;
using MixServer.Application.Queueing.Converters;
using MixServer.Application.Queueing.Responses;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Persistence;
using MixServer.Domain.Queueing.Repositories;
using MixServer.Infrastructure.Users.Repository;

namespace MixServer.Application.Queueing.Queries.GetCurrentQueue;

public class GetCurrentQueueQueryHandler(
    ICurrentUserRepository currentUserRepository,
    IQueueDtoConverter queueConverter,
    IQueueRepository queueRepository,
    IPageConverter pageConverter,
    IUnitOfWork unitOfWork)
    : IQueryHandler<GetCurrentQueueRequest, QueueSnapshotDto>
{
    public async Task<QueueSnapshotDto> HandleAsync(GetCurrentQueueRequest request, CancellationToken cancellationToken = default)
    {
        var page = pageConverter.Convert(request.Page);

        var queue = await queueRepository.GetQueuePageAsync(currentUserRepository.CurrentUserId, page, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        
        return queueConverter.Convert(queue);
    }
}