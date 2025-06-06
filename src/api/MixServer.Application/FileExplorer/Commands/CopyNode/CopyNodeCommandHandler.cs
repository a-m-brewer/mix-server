using FluentValidation;
using MixServer.Application.FileExplorer.Converters;
using MixServer.Domain.FileExplorer.Services;
using MixServer.Domain.Interfaces;

namespace MixServer.Application.FileExplorer.Commands.CopyNode;

public class CopyNodeCommandCommandHandler(
    IFileService fileService,
    INodePathDtoConverter nodePathDtoConverter,
    IValidator<CopyNodeCommand> validator)
    : ICommandHandler<CopyNodeCommand>
{
    public async Task HandleAsync(CopyNodeCommand request, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        fileService.CopyNode(
            nodePathDtoConverter.Convert(request.SourcePath),
            nodePathDtoConverter.Convert(request.DestinationPath),
            request.Move,
            request.Overwrite);
    }
}