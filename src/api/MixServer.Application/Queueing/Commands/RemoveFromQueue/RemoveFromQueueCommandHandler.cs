using FluentValidation;
using MixServer.Application.Queueing.Converters;
using MixServer.Application.Queueing.Responses;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Persistence;
using MixServer.Domain.Queueing.Services;

namespace MixServer.Application.Queueing.Commands.RemoveFromQueue;

public class RemoveFromQueueCommandHandler(
    IQueueDtoConverter queueDtoConverter,
    IValidator<RemoveFromQueueCommand> validator,
    IUserQueueService userQueueService,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RemoveFromQueueCommand, QueuePositionDto>
{
    public async Task<QueuePositionDto> HandleAsync(RemoveFromQueueCommand request, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        userQueueService.RemoveQueueItems(request.QueueItems);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        
        var queuePosition = await userQueueService.GetQueuePositionAsync(cancellationToken: cancellationToken);

        return queueDtoConverter.Convert(queuePosition);
    }
}