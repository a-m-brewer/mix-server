using FluentValidation;

namespace MixServer.Application.Users.Commands.DeleteDevice;

public class DeleteDeviceCommandValidator : AbstractValidator<DeleteDeviceCommand>
{
    public DeleteDeviceCommandValidator()
    {
        RuleFor(r => r.DeviceId)
            .NotEmpty();
    }
}