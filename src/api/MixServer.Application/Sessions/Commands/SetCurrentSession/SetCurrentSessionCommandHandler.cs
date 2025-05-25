using FluentValidation;
using MixServer.Application.FileExplorer.Converters;
using MixServer.Application.Sessions.Converters;
using MixServer.Application.Sessions.Dtos;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Persistence;
using MixServer.Domain.Queueing.Entities;
using MixServer.Domain.Queueing.Services;
using MixServer.Domain.Sessions.Entities;
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
    public async Task<CurrentSessionUpdatedDto> HandleAsync(SetCurrentSessionCommand request)
    {
        await validator.ValidateAndThrowAsync(request);

        var nextSession = await sessionService.AddOrUpdateSessionAsync(new AddOrUpdateSessionRequest
        {
            NodePath = nodePathDtoConverter.Convert(request.NodePath)
        });

        var queueSnapshot = await queueService.SetQueueFolderAsync(nextSession);

        await unitOfWork.SaveChangesAsync();

        return converter.Convert(nextSession, queueSnapshot, true);
    }
}