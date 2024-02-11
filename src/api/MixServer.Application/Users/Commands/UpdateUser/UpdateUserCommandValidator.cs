using FluentValidation;

namespace MixServer.Application.Users.Commands.UpdateUser;

public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        When(x => x.Roles is not null, () =>
        {
            RuleForEach(x => x.Roles)
                .IsInEnum();
        });
    }
}