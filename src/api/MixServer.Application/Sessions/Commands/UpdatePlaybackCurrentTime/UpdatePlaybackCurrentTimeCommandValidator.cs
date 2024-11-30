using FluentValidation;

namespace MixServer.Application.Sessions.Commands.UpdatePlaybackState;

public class UpdatePlaybackStateCommandValidator : AbstractValidator<UpdatePlaybackCurrentTimeCommand>
{
    public UpdatePlaybackStateCommandValidator()
    {
        RuleFor(r => r.CurrentTime)
            .GreaterThanOrEqualTo(TimeSpan.Zero);
    }
}