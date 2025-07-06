using FluentValidation;
using MixServer.Application.FileExplorer.Converters;
using MixServer.Application.Queueing.Responses;
using MixServer.Domain.FileExplorer.Services;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Persistence;
using MixServer.Domain.Queueing.Entities;
using MixServer.Domain.Queueing.Services;

namespace MixServer.Application.Queueing.Commands.AddToQueue;

public class AddToQueueCommandHandler(
    IConverter<QueueSnapshot, QueueSnapshotDto> converter,
    IFileService fileService,
    IQueueService queueService,
    INodePathDtoConverter nodePathDtoConverter,
    IValidator<AddToQueueCommand> validator,
    IUnitOfWork unitOfWork)
    : ICommandHandler<AddToQueueCommand, QueueSnapshotDto>
{
    public async Task<QueueSnapshotDto> HandleAsync(AddToQueueCommand request, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var file = await fileService.GetFileAsync(nodePathDtoConverter.Convert(request.NodePath));

        var queueSnapshot = await queueService.AddToQueueAsync(file, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return converter.Convert(queueSnapshot);
    }
}