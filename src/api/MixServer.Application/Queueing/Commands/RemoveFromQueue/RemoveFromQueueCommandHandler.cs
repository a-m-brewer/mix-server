using FluentValidation;
using MixServer.Application.Queueing.Responses;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Queueing.Entities;
using MixServer.Domain.Queueing.Services;

namespace MixServer.Application.Queueing.Commands.RemoveFromQueue;

public class RemoveFromQueueCommandHandler(
    IConverter<QueueSnapshot, QueueSnapshotDto> converter,
    IQueueService queueService,
    IValidator<RemoveFromQueueCommand> validator)
    : ICommandHandler<RemoveFromQueueCommand, QueueSnapshotDto>
{
    public async Task<QueueSnapshotDto> HandleAsync(RemoveFromQueueCommand request)
    {
        await validator.ValidateAndThrowAsync(request);

        var queueSnapshot = await queueService.RemoveUserQueueItemsAsync(request.QueueItems);

        return converter.Convert(queueSnapshot);
    }
}