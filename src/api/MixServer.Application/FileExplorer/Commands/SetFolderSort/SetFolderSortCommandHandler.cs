using FluentValidation;
using MixServer.Application.FileExplorer.Converters;
using MixServer.Domain.Callbacks;
using MixServer.Domain.FileExplorer.Enums;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Services;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Persistence;
using MixServer.Domain.Queueing.Services;
using MixServer.Infrastructure.Users.Repository;

namespace MixServer.Application.FileExplorer.Commands.SetFolderSort;

public class SetFolderSortCommandHandler(
    ICallbackService callbackService,
    ICurrentUserRepository currentUserRepository,
    IFileService fileService,
    INodePathDtoConverter nodePathDtoConverter,
    IQueueService queueService,
    IUnitOfWork unitOfWork,
    IValidator<SetFolderSortCommand> validator)
    : ICommandHandler<SetFolderSortCommand>
{
    public async Task HandleAsync(SetFolderSortCommand request)
    {
        await validator.ValidateAndThrowAsync(request);
        
        var nodePath = nodePathDtoConverter.Convert(request.NodePath);

        var previousFolder = await fileService.GetFolderAsync(nodePath);

        await fileService.SetFolderSortAsync(new FolderSortRequest
        {
            Path = nodePath,
            Descending = request.Descending,
            SortMode = request.SortMode
        });

        var nextFolder = await fileService.GetFolderAsync(nodePath);

        unitOfWork.OnSaved(() => callbackService.FolderSorted(currentUserRepository.CurrentUserId, nextFolder));
        
        // The folder being sorted is the queues current folder
        var queueFolder = queueService.GetCurrentQueueFolderPath();
        
        if (nextFolder.Node.Path.IsEqualTo(queueFolder) &&
            !previousFolder.Sort.Equals(nextFolder.Sort))
        {
            await queueService.ResortQueueAsync();
        }

        await unitOfWork.SaveChangesAsync();
    }
}