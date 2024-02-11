using FluentValidation;

namespace MixServer.Application.Users.Commands.DeleteUser;

public class DeleteDeviceCommandValidator : AbstractValidator<DeleteUserCommand>
{
    public DeleteDeviceCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();
    }
}