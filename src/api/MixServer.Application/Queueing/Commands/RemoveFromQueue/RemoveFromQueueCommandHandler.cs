using FluentValidation;
using MixServer.Application.Queueing.Converters;
using MixServer.Application.Queueing.Responses;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Persistence;
using MixServer.Domain.Queueing.Repositories;
using MixServer.Infrastructure.Users.Repository;

namespace MixServer.Application.Queueing.Commands.RemoveFromQueue;

public class RemoveFromQueueCommandHandler(
    ICurrentUserRepository currentUserRepository,
    IQueueRepository queueRepository,
    IQueueDtoConverter queueDtoConverter,
    IValidator<RemoveFromQueueCommand> validator,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RemoveFromQueueCommand, QueuePositionDto>
{
    public async Task<QueuePositionDto> HandleAsync(RemoveFromQueueCommand request, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        queueRepository.RemoveQueueItems(currentUserRepository.CurrentUserId, request.QueueItems);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        
        var queuePosition = await queueRepository.GetQueuePositionAsync(currentUserRepository.CurrentUserId, cancellationToken: cancellationToken);

        return queueDtoConverter.Convert(queuePosition);
    }
}