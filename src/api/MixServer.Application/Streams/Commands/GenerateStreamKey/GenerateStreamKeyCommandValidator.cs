using FluentValidation;

namespace MixServer.Application.Streams.Commands.GenerateStreamKey;

public class GenerateStreamKeyCommandValidator : AbstractValidator<GenerateStreamKeyCommand>
{
    public GenerateStreamKeyCommandValidator()
    {
        RuleFor(r => r.PlaybackSessionId)
            .NotEmpty();
    }
}