using FluentValidation;
using MixServer.Domain.Callbacks;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Persistence;
using MixServer.Domain.Queueing.Services;
using MixServer.Domain.Sessions.Requests;
using MixServer.Domain.Sessions.Services;

namespace MixServer.Application.Sessions.Commands.SetCurrentSession;

public class SetCurrentSessionCommandHandler(
    ICallbackService callbackService,
    IQueueService queueService,
    ISessionService sessionService,
    IUnitOfWork unitOfWork,
    IValidator<SetCurrentSessionCommand> validator)
    : ICommandHandler<SetCurrentSessionCommand>
{
    private readonly ICallbackService _callbackService = callbackService;

    public async Task HandleAsync(SetCurrentSessionCommand request)
    {
        await validator.ValidateAndThrowAsync(request);

        var nextSession = await sessionService.AddOrUpdateSessionAsync(new AddOrUpdateSessionRequest
        {
            ParentAbsoluteFilePath = request.AbsoluteFolderPath,
            FileName = request.FileName
        });

        await queueService.SetQueueFolderAsync(nextSession);

        await unitOfWork.SaveChangesAsync();
    }
}