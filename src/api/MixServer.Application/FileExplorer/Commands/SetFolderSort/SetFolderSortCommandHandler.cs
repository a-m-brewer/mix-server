using FluentValidation;
using MixServer.Domain.Callbacks;
using MixServer.Domain.FileExplorer.Services;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Persistence;
using MixServer.Domain.Queueing.Services;
using MixServer.Infrastructure.Users.Repository;

namespace MixServer.Application.FileExplorer.Commands.SetFolderSort;

public class SetFolderSortCommandHandler : ICommandHandler<SetFolderSortCommand>
{
    private readonly ICallbackService _callbackService;
    private readonly ICurrentUserRepository _currentUserRepository;
    private readonly IFileService _fileService;
    private readonly IQueueService _queueService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<SetFolderSortCommand> _validator;

    public SetFolderSortCommandHandler(
        ICallbackService callbackService,
        ICurrentUserRepository currentUserRepository,
        IFileService fileService,
        IQueueService queueService,
        IUnitOfWork unitOfWork,
        IValidator<SetFolderSortCommand> validator)
    {
        _callbackService = callbackService;
        _currentUserRepository = currentUserRepository;
        _fileService = fileService;
        _queueService = queueService;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task HandleAsync(SetFolderSortCommand request)
    {
        await _validator.ValidateAndThrowAsync(request);

        var previousFolder = await _fileService.GetFolderAsync(request.AbsoluteFolderPath);

        await _fileService.SetFolderSortAsync(request);

        var nextFolder = await _fileService.GetFolderAsync(request.AbsoluteFolderPath);

        _unitOfWork.OnSaved(() => _callbackService.FolderSorted(_currentUserRepository.CurrentUserId, nextFolder));
        
        // The folder being sorted is the queues current folder
        if (_queueService.GetCurrentQueueFolderAbsolutePath() == nextFolder.AbsolutePath &&
            !previousFolder.Sort.Equals(nextFolder.Sort))
        {
            await _queueService.ResortQueueAsync();
        }

        await _unitOfWork.SaveChangesAsync();
    }
}