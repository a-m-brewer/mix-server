using FluentValidation;
using MixServer.Application.Sessions.Converters;
using MixServer.Application.Sessions.Dtos;
using MixServer.Domain.Exceptions;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Persistence;
using MixServer.Domain.Queueing.Services;
using MixServer.Domain.Sessions.Requests;
using MixServer.Domain.Sessions.Services;
using MixServer.Infrastructure.Users.Repository;

namespace MixServer.Application.Queueing.Commands.SetQueuePosition;

public class SetQueuePositionCommandHandler(
    IPlaybackSessionDtoConverter converter,
    ISessionService sessionService,
    IValidator<SetQueuePositionCommand> validator,
    IUserQueueService userQueueService,
    IUnitOfWork unitOfWork)
    : ICommandHandler<SetQueuePositionCommand, CurrentSessionUpdatedDto>
{
    public async Task<CurrentSessionUpdatedDto> HandleAsync(SetQueuePositionCommand request, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var item = await userQueueService.SetQueuePositionAsync(request.QueueItemId, cancellationToken);

        if (item.File is null)
        {
            throw new NotFoundException("QueueItemEntity.File", item.FileId?.ToString() ?? "unknown");
        }
        
        var session = await sessionService.AddOrUpdateSessionAsync(new AddOrUpdateSessionRequest
        {
            NodePath = item.File.Path
        }, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        
        var queuePosition = await userQueueService.GetQueuePositionAsync(cancellationToken: cancellationToken);

        return converter.Convert(session, queuePosition, true);
    }
}