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
    IQueueService queueService,
    ISessionService sessionService,
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

        var queueSnapshot = await queueService.SetQueueFolderAsync(nextSession, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return converter.Convert(nextSession, queueSnapshot, true);
    }
}