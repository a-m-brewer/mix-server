using FluentValidation;
using MixServer.Domain.FileExplorer.Services;
using MixServer.Domain.Interfaces;

namespace MixServer.Application.FileExplorer.Commands.DeleteNode;

public class DeleteNodeCommandHandler(
    IFileService fileService,
    IValidator<DeleteNodeCommand> validator)
    : ICommandHandler<DeleteNodeCommand>
{
    public async Task HandleAsync(DeleteNodeCommand request)
    {
        await validator.ValidateAndThrowAsync(request);
        
        fileService.DeleteNode(request.AbsolutePath);
    }
}