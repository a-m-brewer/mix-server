using FluentValidation;
using MixServer.Application.FileExplorer.Converters;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Services;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Persistence;
using MixServer.Domain.Queueing.Repositories;
using MixServer.Domain.Queueing.Services;
using MixServer.Domain.Users.Repositories;
using MixServer.Infrastructure.Users.Repository;

namespace MixServer.Application.FileExplorer.Commands.SetFolderSort;

public class SetFolderSortCommandHandler(
    ICurrentUserRepository currentUserRepository,
    IFileService fileService,
    INodePathDtoConverter nodePathDtoConverter,
    IUserQueueService userQueueService,
    IUnitOfWork unitOfWork,
    IValidator<SetFolderSortCommand> validator)
    : ICommandHandler<SetFolderSortCommand>
{
    public async Task HandleAsync(SetFolderSortCommand request, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        
        var nodePath = nodePathDtoConverter.Convert(request.NodePath);

        await fileService.SetFolderSortAsync(new FolderSortRequest
        {
            Path = nodePath,
            Descending = request.Descending,
            SortMode = request.SortMode,
        }, cancellationToken);

        unitOfWork.InvokeCallbackOnSaved(cb => cb.FolderSorted(currentUserRepository.CurrentUserId, nodePath));
        
        // The folder being sorted is the queues current folder
        var queueFolder = await userQueueService.GetQueueCurrentFolderAsync(cancellationToken);
        
        if (queueFolder is not null && nodePath.IsEqualTo(queueFolder.Path))
        {
            await userQueueService.RefreshQueueAsync(cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}