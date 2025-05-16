using FluentValidation;
using MixServer.Application.Queueing.Responses;
using MixServer.Domain.FileExplorer.Services;
using MixServer.Domain.Queueing.Entities;
using MixServer.Domain.Queueing.Services;
using MixServer.Shared.Interfaces;

namespace MixServer.Application.Queueing.Commands.AddToQueue;

public class AddToQueueCommandHandler(
    IConverter<QueueSnapshot, QueueSnapshotDto> converter,
    IFileService fileService,
    IQueueService queueService,
    IValidator<AddToQueueCommand> validator)
    : ICommandHandler<AddToQueueCommand, QueueSnapshotDto>
{
    public async Task<QueueSnapshotDto> HandleAsync(AddToQueueCommand request)
    {
        await validator.ValidateAndThrowAsync(request);

        var file = fileService.GetFile(request.AbsoluteFolderPath, request.FileName);

        var queueSnapshot = await queueService.AddToQueueAsync(file);

        return converter.Convert(queueSnapshot);
    }
}