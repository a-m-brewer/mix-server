using FluentValidation;
using MixServer.Domain.FileExplorer.Services;
using MixServer.Domain.Interfaces;

namespace MixServer.Application.FileExplorer.Commands.CopyNode;

public class CopyNodeCommandCommandHandler(
    IFileService fileService,
    IValidator<CopyNodeCommand> validator)
    : ICommandHandler<CopyNodeCommand>
{
    public async Task HandleAsync(CopyNodeCommand request)
    {
        await validator.ValidateAndThrowAsync(request);

        fileService.CopyNode(
            request.SourceAbsolutePath,
            request.DestinationFolder,
            request.DestinationName,
            request.Move,
            request.Overwrite);
    }
}