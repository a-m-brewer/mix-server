using FluentValidation;

namespace MixServer.Application.Sessions.Commands.SeekCommand;

public class SeekCommandValidator : AbstractValidator<SeekCommand>
{
    public SeekCommandValidator()
    {
        RuleFor(r => r.Time)
            .GreaterThanOrEqualTo(TimeSpan.Zero);
    }
}