using FluentValidation;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Queueing.Services;

namespace MixServer.Application.Queueing.Commands.RemoveFromQueue;

public class RemoveFromQueueCommandHandler(
    IQueueService queueService,
    IValidator<RemoveFromQueueCommand> validator)
    : ICommandHandler<RemoveFromQueueCommand>
{
    public async Task HandleAsync(RemoveFromQueueCommand request)
    {
        await validator.ValidateAndThrowAsync(request);

        await queueService.RemoveUserQueueItemsAsync(request.QueueItems);
    }
}