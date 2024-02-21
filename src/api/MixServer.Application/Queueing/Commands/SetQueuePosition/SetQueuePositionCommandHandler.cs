using FluentValidation;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Persistence;
using MixServer.Domain.Queueing.Services;
using MixServer.Domain.Sessions.Requests;
using MixServer.Domain.Sessions.Services;

namespace MixServer.Application.Queueing.Commands.SetQueuePosition;

public class SetQueuePositionCommandHandler(
    ISessionService sessionService,
    IQueueService queueService,
    IValidator<SetQueuePositionCommand> validator,
    IUnitOfWork unitOfWork)
    : ICommandHandler<SetQueuePositionCommand>
{
    public async Task HandleAsync(SetQueuePositionCommand request)
    {
        await validator.ValidateAndThrowAsync(request);

        await queueService.SetQueuePositionAsync(request.QueueItemId);

        var file = await queueService.GetCurrentPositionFileOrThrowAsync();
        
        await sessionService.AddOrUpdateSessionAsync(new AddOrUpdateSessionRequest
        {
            ParentAbsoluteFilePath = file.Parent.AbsolutePath,
            FileName = file.Name
        });

        await unitOfWork.SaveChangesAsync();
    }
}