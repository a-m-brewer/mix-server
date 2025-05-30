using FluentValidation;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Persistence;
using MixServer.Domain.Users.Services;

namespace MixServer.Application.Devices.Commands.DeleteDevice;

public class DeleteDeviceCommandHandler(
    IDeviceService deviceService,
    IValidator<DeleteDeviceCommand> validator,
    IUnitOfWork unitOfWork)
    : ICommandHandler<DeleteDeviceCommand>
{
    public async Task HandleAsync(DeleteDeviceCommand request, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        await deviceService.DeleteDeviceAsync(request.DeviceId, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}