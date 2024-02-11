using FluentValidation;

namespace MixServer.Application.Users.Commands.RefreshUser;

public class RefreshUserCommandValidator : AbstractValidator<RefreshUserCommand>
{
    public RefreshUserCommandValidator()
    {
        RuleFor(r => r.AccessToken)
            .NotEmpty();

        RuleFor(r => r.RefreshToken)
            .NotEmpty();

        RuleFor(r => r.DeviceId)
            .NotEmpty();
    }
}