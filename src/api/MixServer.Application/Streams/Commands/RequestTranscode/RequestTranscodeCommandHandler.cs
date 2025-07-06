using FluentValidation;
using MixServer.Application.FileExplorer.Converters;
using MixServer.Domain.Exceptions;
using MixServer.Domain.FileExplorer.Entities;
using MixServer.Domain.FileExplorer.Repositories;
using MixServer.Domain.FileExplorer.Repositories.DbQueryOptions;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Streams.Caches;
using MixServer.Domain.Streams.Enums;
using MixServer.Domain.Streams.Services;

namespace MixServer.Application.Streams.Commands.RequestTranscode;

public class RequestTranscodeCommandHandler(
    IFileExplorerNodeRepository fileExplorerNodeRepository,
    INodePathDtoConverter nodePathDtoConverter,
    ITranscodeService transcodeService,
    ITranscodeCache transcodeCache,
    IValidator<RequestTranscodeCommand> validator)
    : ICommandHandler<RequestTranscodeCommand>
{
    public async Task HandleAsync(RequestTranscodeCommand request, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        
        var nodePath = nodePathDtoConverter.Convert(request.NodePath);

        var file = await fileExplorerNodeRepository.GetFileNodeAsync(nodePath, new GetFileQueryOptions
        {
            IncludeMetadata = true,
            IncludeTranscode = true
        }, cancellationToken);

        if (!file.Exists)
        {
            throw new NotFoundException(nameof(request.NodePath), nodePath.AbsolutePath);
        }

        if (!file.PlaybackSupported)
        {
            throw new InvalidRequestException(nameof(request.NodePath), $"{nodePath.AbsolutePath} is not supported for transcoding");
        }

        if (file.Transcode is not null && transcodeCache.GetTranscodeStatus(file.Transcode.Id) != TranscodeState.None)
        {
            throw new InvalidRequestException(nameof(request.NodePath), $"{nodePath.AbsolutePath} is already being transcoded");
        }
        
        var bitrate = file.Metadata is MediaMetadataEntity mediaMetadataEntity
            ? mediaMetadataEntity.Bitrate
            : 0;
        await transcodeService.RequestTranscodeAsync(file, bitrate, cancellationToken);
    }
}