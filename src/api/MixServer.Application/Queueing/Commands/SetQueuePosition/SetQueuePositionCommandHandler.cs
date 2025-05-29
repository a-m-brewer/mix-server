using FluentValidation;
using MixServer.Application.Sessions.Converters;
using MixServer.Application.Sessions.Dtos;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Persistence;
using MixServer.Domain.Queueing.Services;
using MixServer.Domain.Sessions.Requests;
using MixServer.Domain.Sessions.Services;

namespace MixServer.Application.Queueing.Commands.SetQueuePosition;

public class SetQueuePositionCommandHandler(
    IPlaybackSessionDtoConverter converter,
    ISessionService sessionService,
    IQueueService queueService,
    IValidator<SetQueuePositionCommand> validator,
    IUnitOfWork unitOfWork)
    : ICommandHandler<SetQueuePositionCommand, CurrentSessionUpdatedDto>
{
    public async Task<CurrentSessionUpdatedDto> HandleAsync(SetQueuePositionCommand request, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var queueSnapshot = await queueService.SetQueuePositionAsync(request.QueueItemId);

        var file = await queueService.GetCurrentPositionFileOrThrowAsync();
        
        var session = await sessionService.AddOrUpdateSessionAsync(new AddOrUpdateSessionRequest
        {
            NodePath = file.Path
        }, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return converter.Convert(session, queueSnapshot, true);
    }
}