using FluentValidation;

namespace MixServer.Application.Sessions.Commands.UpdatePlaybackCurrentTime;

public class UpdatePlaybackCurrentTimeCommandValidator : AbstractValidator<UpdatePlaybackCurrentTimeCommand>
{
    public UpdatePlaybackCurrentTimeCommandValidator()
    {
        RuleFor(r => r.CurrentTime)
            .GreaterThanOrEqualTo(TimeSpan.Zero);
    }
}