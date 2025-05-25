using FluentValidation;
using MixServer.Application.FileExplorer.Converters;
using MixServer.Domain.FileExplorer.Services;
using MixServer.Domain.Interfaces;

namespace MixServer.Application.FileExplorer.Commands.DeleteNode;

public class DeleteNodeCommandHandler(
    IFileService fileService,
    INodePathDtoConverter nodePathDtoConverter,
    IValidator<DeleteNodeCommand> validator)
    : ICommandHandler<DeleteNodeCommand>
{
    public async Task HandleAsync(DeleteNodeCommand request)
    {
        await validator.ValidateAndThrowAsync(request);
        
        fileService.DeleteNode(nodePathDtoConverter.Convert(request.NodePath));
    }
}