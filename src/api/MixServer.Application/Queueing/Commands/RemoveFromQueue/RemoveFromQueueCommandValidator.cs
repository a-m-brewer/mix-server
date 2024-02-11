using FluentValidation;

namespace MixServer.Application.Queueing.Commands.RemoveFromQueue;

public class RemoveFromQueueCommandValidator : AbstractValidator<RemoveFromQueueCommand>
{
    public RemoveFromQueueCommandValidator()
    {
        RuleFor(r => r.QueueItems)
            .NotEmpty();

        RuleForEach(r => r.QueueItems)
            .NotEmpty();
    }
}