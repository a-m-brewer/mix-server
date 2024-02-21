using FluentValidation;
using MixServer.Domain.FileExplorer.Services;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Queueing.Services;

namespace MixServer.Application.Queueing.Commands.AddToQueue;

public class AddToQueueCommandHandler(
    IFileService fileService,
    IQueueService queueService,
    IValidator<AddToQueueCommand> validator)
    : ICommandHandler<AddToQueueCommand>
{
    public async Task HandleAsync(AddToQueueCommand request)
    {
        await validator.ValidateAndThrowAsync(request);

        var file = fileService.GetFile(request.AbsoluteFolderPath, request.FileName);

        await queueService.AddToQueueAsync(file);
    }
}