using FluentValidation;

namespace MixServer.Application.Queueing.Commands.AddToQueue;

public class AddToQueueCommandValidator : AbstractValidator<AddToQueueCommand>
{
    public AddToQueueCommandValidator()
    {
        RuleFor(r => r.NodePath)
            .NotEmpty();
    }
}