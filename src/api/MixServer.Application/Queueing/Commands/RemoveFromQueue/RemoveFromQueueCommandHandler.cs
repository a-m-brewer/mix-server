using FluentValidation;
using MixServer.Application.Queueing.Responses;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Persistence;
using MixServer.Domain.Queueing.Entities;
using MixServer.Domain.Queueing.Services;

namespace MixServer.Application.Queueing.Commands.RemoveFromQueue;

public class RemoveFromQueueCommandHandler(
    IConverter<QueueSnapshot, QueueSnapshotDto> converter,
    IQueueService queueService,
    IValidator<RemoveFromQueueCommand> validator,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RemoveFromQueueCommand, QueueSnapshotDto>
{
    public async Task<QueueSnapshotDto> HandleAsync(RemoveFromQueueCommand request, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var queueSnapshot = await queueService.RemoveUserQueueItemsAsync(request.QueueItems, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return converter.Convert(queueSnapshot);
    }
}