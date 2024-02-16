using FluentValidation;
using MixServer.Domain.Callbacks;
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
    IQueueService queueService,
    IUnitOfWork unitOfWork,
    IValidator<SetFolderSortCommand> validator)
    : ICommandHandler<SetFolderSortCommand>
{
    public async Task HandleAsync(SetFolderSortCommand request)
    {
        await validator.ValidateAndThrowAsync(request);

        var previousFolder = await fileService.GetFolderAsync(request.AbsoluteFolderPath);

        await fileService.SetFolderSortAsync(request);

        var nextFolder = await fileService.GetFolderAsync(request.AbsoluteFolderPath);

        unitOfWork.OnSaved(() => callbackService.FolderSorted(currentUserRepository.CurrentUserId, nextFolder));
        
        // The folder being sorted is the queues current folder
        if (queueService.GetCurrentQueueFolderAbsolutePath() == nextFolder.Node.AbsolutePath &&
            !previousFolder.Sort.Equals(nextFolder.Sort))
        {
            await queueService.ResortQueueAsync();
        }

        await unitOfWork.SaveChangesAsync();
    }
}