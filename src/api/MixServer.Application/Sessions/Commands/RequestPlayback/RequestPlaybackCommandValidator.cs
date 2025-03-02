using FluentValidation;

namespace MixServer.Application.Sessions.Commands.RequestPlayback;

public class RequestPlaybackCommandValidator : AbstractValidator<RequestPlaybackCommand>
{
    public RequestPlaybackCommandValidator()
    {
        When(w => w.DeviceId.HasValue, () =>
        {
            RuleFor(w => w.DeviceId).NotEmpty();
        });
    }
}