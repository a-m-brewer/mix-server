using FluentValidation;
using MixServer.Application.FileExplorer.Converters;
using MixServer.Application.Queueing.Converters;
using MixServer.Application.Queueing.Responses;
using MixServer.Domain.FileExplorer.Services;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Persistence;
using MixServer.Domain.Queueing.Entities;
using MixServer.Domain.Queueing.Repositories;
using MixServer.Domain.Queueing.Services;
using MixServer.Infrastructure.Users.Repository;

namespace MixServer.Application.Queueing.Commands.AddToQueue;

public class AddToQueueCommandHandler(
    IUserQueueService userQueueService,
    INodePathDtoConverter nodePathDtoConverter,
    IQueueDtoConverter queueDtoConverter,
    IValidator<AddToQueueCommand> validator,
    IUnitOfWork unitOfWork)
    : ICommandHandler<AddToQueueCommand, QueuePositionDto>
{
    public async Task<QueuePositionDto> HandleAsync(AddToQueueCommand request, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var nodePath = nodePathDtoConverter.Convert(request.NodePath);

        await userQueueService.AddFileAsync(nodePath, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        
        var currentPosition = await userQueueService.GetQueuePositionAsync(cancellationToken);

        return queueDtoConverter.Convert(currentPosition);
    }
}