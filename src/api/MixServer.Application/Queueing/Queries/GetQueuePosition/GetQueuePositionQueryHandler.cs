using MixServer.Application.Queueing.Converters;
using MixServer.Application.Queueing.Responses;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Queueing.Services;
using MixServer.Infrastructure.Users.Repository;

namespace MixServer.Application.Queueing.Queries.GetQueuePosition;

public class GetQueuePositionQueryHandler(
    IUserQueueService userQueueService,
    IQueueDtoConverter queueDtoConverter) : IQueryHandler<QueuePositionDto>
{
    public async Task<QueuePositionDto> HandleAsync(CancellationToken cancellationToken = default)
    {
        var position = await userQueueService.GetQueuePositionAsync(cancellationToken: cancellationToken);

        return queueDtoConverter.Convert(position);
    }
}