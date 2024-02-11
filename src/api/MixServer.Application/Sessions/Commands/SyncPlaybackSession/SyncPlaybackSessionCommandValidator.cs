using FluentValidation;

namespace MixServer.Application.Sessions.Commands.SyncPlaybackSession;

public class SyncPlaybackSessionCommandValidator : AbstractValidator<SyncPlaybackSessionCommand>
{
    public SyncPlaybackSessionCommandValidator()
    {
        When(w => w.PlaybackSessionId.HasValue, () =>
        {
            RuleFor(r => r.PlaybackSessionId)
                .NotEmpty();
        });

        RuleFor(r => r.CurrentTime)
            .GreaterThanOrEqualTo(0);
    }
}