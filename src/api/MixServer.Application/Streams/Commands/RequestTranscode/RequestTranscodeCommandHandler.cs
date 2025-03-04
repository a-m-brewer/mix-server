using FluentValidation;
using MixServer.Domain.Exceptions;
using MixServer.Domain.FileExplorer.Services;
using MixServer.Domain.FileExplorer.Services.Caching;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Streams.Caches;
using MixServer.Domain.Streams.Enums;
using MixServer.Domain.Streams.Repositories;
using MixServer.Domain.Streams.Services;

namespace MixServer.Application.Streams.Commands.RequestTranscode;

public class RequestTranscodeCommandHandler(
    IFileService fileService,
    IMediaInfoCache mediaInfoCache,
    ITranscodeService transcodeService,
    ITranscodeCache transcodeCache,
    ITranscodeRepository transcodeRepository,
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

        if (!file.PlaybackSupported || !file.Metadata.IsMedia)
        {
            throw new InvalidRequestException(nameof(request.AbsoluteFilePath), $"{request.AbsoluteFilePath} is not supported for transcoding");
        }

        var existingTranscode = await transcodeRepository.GetOrDefaultAsync(file.AbsolutePath);

        if (existingTranscode is not null && transcodeCache.GetTranscodeStatus(existingTranscode.Id) != TranscodeState.None)
        {
            throw new InvalidRequestException(nameof(request.AbsoluteFilePath), $"{request.AbsoluteFilePath} is already being transcoded");
        }
        
        var bitrate = mediaInfoCache.TryGet(file.AbsolutePath, out var mediaInfo) ? mediaInfo.Bitrate : 0;
        await transcodeService.RequestTranscodeAsync(file.AbsolutePath, bitrate);
    }
}