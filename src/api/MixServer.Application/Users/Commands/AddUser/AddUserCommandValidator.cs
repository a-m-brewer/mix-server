using FluentValidation;
using MixServer.Domain.Users.Enums;

namespace MixServer.Application.Users.Commands.AddUser;

public class AddUserCommandValidator : AbstractValidator<AddUserCommand>
{
    public AddUserCommandValidator()
    {
        RuleFor(r => r.Username)
            .NotEmpty();

        RuleForEach(r => r.Roles)
            .NotEqual(Role.Owner)
            .WithMessage("Owner role can not be assigned to new users");
    }
}