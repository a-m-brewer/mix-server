using FluentValidation;
using MixServer.Application.FileExplorer.Converters;
using MixServer.Application.Sessions.Converters;
using MixServer.Application.Sessions.Dtos;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Persistence;
using MixServer.Domain.Queueing.Services;
using MixServer.Domain.Sessions.Requests;
using MixServer.Domain.Sessions.Services;

namespace MixServer.Application.Sessions.Commands.SetCurrentSession;

public class SetCurrentSessionCommandHandler(
    IPlaybackSessionDtoConverter converter,
    INodePathDtoConverter nodePathDtoConverter,
    ISessionService sessionService,
    IUserQueueService queueService,
    IUnitOfWork unitOfWork,
    IValidator<SetCurrentSessionCommand> validator)
    : ICommandHandler<SetCurrentSessionCommand, CurrentSessionUpdatedDto>
{
    public async Task<CurrentSessionUpdatedDto> HandleAsync(SetCurrentSessionCommand request, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var nextSession = await sessionService.AddOrUpdateSessionAsync(new AddOrUpdateSessionRequest
        {
            NodePath = nodePathDtoConverter.Convert(request.NodePath)
        }, cancellationToken);

        var parentId = nextSession.NodeEntity.ParentId ?? nextSession.NodeEntity.RootChildId;
        await queueService.SetFolderAsync(parentId, cancellationToken);
        await queueService.SetQueuePositionByFileIdAsync(nextSession.NodeIdEntity, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var queuePosition = await queueService.GetQueuePositionAsync(cancellationToken: cancellationToken);

        return converter.Convert(nextSession, queuePosition, true);
    }
}