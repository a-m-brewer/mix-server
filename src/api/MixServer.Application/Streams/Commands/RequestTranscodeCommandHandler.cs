using FluentValidation;
using MixServer.Domain.Exceptions;
using MixServer.Domain.FileExplorer.Models.Metadata;
using MixServer.Domain.FileExplorer.Services;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Streams.Caches;
using MixServer.Domain.Streams.Enums;
using MixServer.Domain.Streams.Services;

namespace MixServer.Application.Streams.Commands;

public class RequestTranscodeCommandHandler(
    IFileService fileService,
    ITranscodeService transcodeService,
    ITranscodeCache transcodeCache,
    IValidator<RequestTranscodeCommand> validator)
    : ICommandHandler<RequestTranscodeCommand>
{
    public async Task HandleAsync(RequestTranscodeCommand request)
    {
        await validator.ValidateAndThrowAsync(request);
        
        var file = fileService.GetFile(request.AbsoluteFilePath);

        if (!file.Exists)
        {
            throw new NotFoundException(nameof(request.AbsoluteFilePath), request.AbsoluteFilePath);
        }

        if (!file.PlaybackSupported || file.Metadata is not IMediaMetadata mediaMetadata)
        {
            throw new InvalidRequestException(nameof(request.AbsoluteFilePath), $"{request.AbsoluteFilePath} is not supported for transcoding");
        }

        if (transcodeCache.GetTranscodeStatus(mediaMetadata.FileHash) != TranscodeState.None)
        {
            throw new InvalidRequestException(nameof(request.AbsoluteFilePath), $"{request.AbsoluteFilePath} is already being transcoded");
        }
        
        transcodeService.RequestTranscode(file.AbsolutePath, mediaMetadata);
    }
}