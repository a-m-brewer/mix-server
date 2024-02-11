using FluentValidation;
using MixServer.Domain.Callbacks;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Persistence;
using MixServer.Domain.Queueing.Services;
using MixServer.Domain.Sessions.Requests;
using MixServer.Domain.Sessions.Services;

namespace MixServer.Application.Sessions.Commands.SetCurrentSession;

public class SetCurrentSessionCommandHandler : ICommandHandler<SetCurrentSessionCommand>
{
    private readonly ICallbackService _callbackService;
    private readonly IQueueService _queueService;
    private readonly ISessionService _sessionService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<SetCurrentSessionCommand> _validator;

    public SetCurrentSessionCommandHandler(
        ICallbackService callbackService,
        IQueueService queueService,
        ISessionService sessionService,
        IUnitOfWork unitOfWork,
        IValidator<SetCurrentSessionCommand> validator)
    {
        _callbackService = callbackService;
        _queueService = queueService;
        _sessionService = sessionService;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }
    
    public async Task HandleAsync(SetCurrentSessionCommand request)
    {
        await _validator.ValidateAndThrowAsync(request);

        var nextSession = await _sessionService.AddOrUpdateSessionAsync(new AddOrUpdateSessionRequest
        {
            ParentAbsoluteFilePath = request.AbsoluteFolderPath,
            FileName = request.FileName
        });

        await _queueService.SetQueueFolderAsync(nextSession);

        await _unitOfWork.SaveChangesAsync();
    }
}