using FluentValidation;

namespace MixServer.Application.Queueing.Commands.SetQueuePosition;

public class SetQueuePositionCommandValidator : AbstractValidator<SetQueuePositionCommand>
{
    public SetQueuePositionCommandValidator()
    {
        RuleFor(r => r.QueueItemId)
            .NotEmpty();
    }
}