using MixServer.Application.Queueing.Converters;
using MixServer.Application.Queueing.Responses;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Queueing.Repositories;
using MixServer.Infrastructure.Users.Repository;

namespace MixServer.Application.Queueing.Queries.GetQueuePosition;

public class GetQueuePositionQueryHandler(
    ICurrentUserRepository currentUserRepository,
    IQueueDtoConverter queueDtoConverter,
    IQueueRepository queueRepository) : IQueryHandler<QueuePositionDto>
{
    public async Task<QueuePositionDto> HandleAsync(CancellationToken cancellationToken = default)
    {
        var position = await queueRepository.GetQueuePositionAsync(currentUserRepository.CurrentUserId, cancellationToken: cancellationToken);

        return queueDtoConverter.Convert(position);
    }
}