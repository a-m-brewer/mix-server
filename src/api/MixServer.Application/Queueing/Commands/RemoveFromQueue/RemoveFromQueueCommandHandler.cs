using FluentValidation;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Queueing.Services;

namespace MixServer.Application.Queueing.Commands.RemoveFromQueue;

public class RemoveFromQueueCommandHandler : ICommandHandler<RemoveFromQueueCommand>
{
    private readonly IQueueService _queueService;
    private readonly IValidator<RemoveFromQueueCommand> _validator;

    public RemoveFromQueueCommandHandler(
        IQueueService queueService,
        IValidator<RemoveFromQueueCommand> validator)
    {
        _queueService = queueService;
        _validator = validator;
    }
    
    public async Task HandleAsync(RemoveFromQueueCommand request)
    {
        await _validator.ValidateAndThrowAsync(request);

        await _queueService.RemoveUserQueueItemsAsync(request.QueueItems);
    }
}