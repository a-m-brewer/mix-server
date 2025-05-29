using FluentValidation;
using MixServer.Application.FileExplorer.Converters;
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
    IFolderPersistenceService folderPersistenceService,
    IMediaInfoCache mediaInfoCache,
    INodePathDtoConverter nodePathDtoConverter,
    ITranscodeService transcodeService,
    ITranscodeCache transcodeCache,
    ITranscodeRepository transcodeRepository,
    IValidator<RequestTranscodeCommand> validator)
    : ICommandHandler<RequestTranscodeCommand>
{
    public async Task HandleAsync(RequestTranscodeCommand request, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        
        var nodePath = nodePathDtoConverter.Convert(request.NodePath);
        
        var file = await folderPersistenceService.GetFileAsync(nodePath);

        if (!file.Exists)
        {
            throw new NotFoundException(nameof(request.NodePath), nodePath.AbsolutePath);
        }

        if (!file.PlaybackSupported || !file.Metadata.IsMedia)
        {
            throw new InvalidRequestException(nameof(request.NodePath), $"{nodePath.AbsolutePath} is not supported for transcoding");
        }

        var existingTranscode = await transcodeRepository.GetOrDefaultAsync(file.Path);

        if (existingTranscode is not null && transcodeCache.GetTranscodeStatus(existingTranscode.Id) != TranscodeState.None)
        {
            throw new InvalidRequestException(nameof(request.NodePath), $"{nodePath.AbsolutePath} is already being transcoded");
        }
        
        var bitrate = mediaInfoCache.TryGet(file.Path, out var mediaInfo) ? mediaInfo.Bitrate : 0;
        await transcodeService.RequestTranscodeAsync(file.Entity, bitrate);
    }
}