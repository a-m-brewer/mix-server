using FluentValidation;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Persistence;
using MixServer.Domain.Users.Services;

namespace MixServer.Application.Users.Commands.DeleteDevice;

public class DeleteDeviceCommandHandler : ICommandHandler<DeleteDeviceCommand>
{
    private readonly IDeviceService _deviceService;
    private readonly IValidator<DeleteDeviceCommand> _validator;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteDeviceCommandHandler(
        IDeviceService deviceService,
        IValidator<DeleteDeviceCommand> validator,
        IUnitOfWork unitOfWork)
    {
        _deviceService = deviceService;
        _validator = validator;
        _unitOfWork = unitOfWork;
    }
    
    public async Task HandleAsync(DeleteDeviceCommand request)
    {
        await _validator.ValidateAndThrowAsync(request);

        await _deviceService.DeleteDeviceAsync(request.DeviceId);

        await _unitOfWork.SaveChangesAsync();
    }
}