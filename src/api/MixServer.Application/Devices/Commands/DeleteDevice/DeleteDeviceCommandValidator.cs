using FluentValidation;

namespace MixServer.Application.Devices.Commands.DeleteDevice;

public class DeleteDeviceCommandValidator : AbstractValidator<DeleteDeviceCommand>
{
    public DeleteDeviceCommandValidator()
    {
        RuleFor(r => r.DeviceId)
            .NotEmpty();
    }
}