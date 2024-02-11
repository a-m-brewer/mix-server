using FluentValidation;

namespace MixServer.Application.Users.Commands.ResetPassword;

public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(r => r.CurrentPassword)
            .NotEmpty();

        RuleFor(r => r.NewPassword)
            .NotEmpty();

        RuleFor(r => r.NewPasswordConfirmation)
            .Equal(v => v.NewPassword);
    }
}