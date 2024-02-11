using FluentValidation;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Persistence;
using MixServer.Domain.Queueing.Services;
using MixServer.Domain.Sessions.Requests;
using MixServer.Domain.Sessions.Services;

namespace MixServer.Application.Queueing.Commands.SetQueuePosition;

public class SetQueuePositionCommandHandler : ICommandHandler<SetQueuePositionCommand>
{
    private readonly ISessionService _sessionService;
    private readonly IQueueService _queueService;
    private readonly IValidator<SetQueuePositionCommand> _validator;
    private readonly IUnitOfWork _unitOfWork;

    public SetQueuePositionCommandHandler(
        ISessionService sessionService,
        IQueueService queueService,
        IValidator<SetQueuePositionCommand> validator,
        IUnitOfWork unitOfWork)
    {
        _sessionService = sessionService;
        _queueService = queueService;
        _validator = validator;
        _unitOfWork = unitOfWork;
    }
    
    public async Task HandleAsync(SetQueuePositionCommand request)
    {
        await _validator.ValidateAndThrowAsync(request);

        await _queueService.SetQueuePositionAsync(request.QueueItemId);

        var file = await _queueService.GetCurrentPositionFileOrThrowAsync();
        
        await _sessionService.AddOrUpdateSessionAsync(new AddOrUpdateSessionRequest
        {
            ParentAbsoluteFilePath = file.Parent.AbsolutePath,
            FileName = file.Name
        });

        await _unitOfWork.SaveChangesAsync();
    }
}