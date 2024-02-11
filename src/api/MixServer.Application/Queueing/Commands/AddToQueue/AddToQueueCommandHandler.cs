using FluentValidation;
using MixServer.Domain.FileExplorer.Services;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Queueing.Services;

namespace MixServer.Application.Queueing.Commands.AddToQueue;

public class AddToQueueCommandHandler : ICommandHandler<AddToQueueCommand>
{
    private readonly IFileService _fileService;
    private readonly IQueueService _queueService;
    private readonly IValidator<AddToQueueCommand> _validator;

    public AddToQueueCommandHandler(
        IFileService fileService,
        IQueueService queueService,
        IValidator<AddToQueueCommand> validator)
    {
        _fileService = fileService;
        _queueService = queueService;
        _validator = validator;
    }
    
    public async Task HandleAsync(AddToQueueCommand request)
    {
        await _validator.ValidateAndThrowAsync(request);

        var file = _fileService.GetFile(request.AbsoluteFolderPath, request.FileName);

        await _queueService.AddToQueueAsync(file);
    }
}