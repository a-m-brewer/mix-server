using FluentValidation;

namespace MixServer.Application.Sessions.Commands.SetNextSession;

public class SetNextSessionCommandValidator : AbstractValidator<SetNextSessionCommand>
{
    public SetNextSessionCommandValidator()
    {
        RuleFor(r => r.Offset)
            .NotEqual(0);
    }
}