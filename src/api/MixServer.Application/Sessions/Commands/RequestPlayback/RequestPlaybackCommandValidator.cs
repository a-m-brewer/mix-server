using FluentValidation;

namespace MixServer.Application.Sessions.Commands.RequestPlayback;

public class RequestPlaybackCommandValidator : AbstractValidator<RequestPlaybackCommand>
{
    public RequestPlaybackCommandValidator()
    {
        RuleFor(r => r.DeviceId)
            .NotEmpty();
    }
}