using FluentValidation;

namespace MixServer.Application.Users.Commands.LoginUser;

public class LoginUserCommandValidator : AbstractValidator<LoginUserCommand>
{
    public LoginUserCommandValidator()
    {
        RuleFor(r => r.Username)
            .NotEmpty();

        RuleFor(r => r.Password)
            .NotEmpty();

        When(w => w.DeviceId.HasValue, () =>
        {
            RuleFor(r => r.DeviceId)
                .NotEmpty();
        });
    }
}