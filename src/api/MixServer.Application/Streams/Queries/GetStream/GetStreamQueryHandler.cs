using FluentValidation;
using MixServer.Domain.Exceptions;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Services;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Sessions.Accessors;
using MixServer.Domain.Sessions.Entities;
using MixServer.Domain.Sessions.Services;
using MixServer.Domain.Streams.Caches;
using MixServer.Domain.Streams.Models;
using MixServer.Domain.Streams.Repositories;
using MixServer.Domain.Users.Services;
using MixServer.Infrastructure.Users.Services;

namespace MixServer.Application.Streams.Queries.GetStream;

public class GetStreamQueryHandler(
    IDeviceTrackingService deviceTrackingService,
    IJwtService jwtService,
    IMimeTypeService mimeTypeService,
    ISessionService sessionService,
    ITranscodeCache transcodeCache,
    ITranscodeRepository transcodeRepository,
    IRootFileExplorerFolder rootFileExplorerFolder,
    IValidator<GetStreamQuery> validator)
    : IQueryHandler<GetStreamQuery, StreamFile>
{
    public async Task<StreamFile> HandleAsync(GetStreamQuery query)
    {
        await validator.ValidateAndThrowAsync(query);

        var file = Guid.TryParse(query.Id, out var playbackSessionId)
            ? await GetPlaybackSessionAsync(playbackSessionId, query.SecurityParameters)
            : GetSegment(query.Id);

        return file;
    }

    private HlsSegmentStreamFile GetSegment(string segment)
    {
        if (!segment.EndsWith(".ts"))
        {
            throw new InvalidRequestException("Invalid segment request");
        }

        return transcodeCache.GetSegmentOrThrow(segment);
    }

    private async Task<StreamFile> GetPlaybackSessionAsync(Guid playbackSessionId, StreamSecurityParametersDto securityParameters)
    {
        jwtService.ValidateKeyOrThrow(playbackSessionId.ToString(), securityParameters.Key, securityParameters.Expires);
        
        var session = await sessionService.GetPlaybackSessionByIdAsync(playbackSessionId);

        if (session.File is null || !session.File.Exists)
        {
            throw new NotFoundException(nameof(PlaybackSession), session.AbsolutePath);
        }

        // Specifically use the RequestDevice to determine if the file can be played
        // Because if playback device is switched we want the client to be seeing what version they can play
        // Rather than what the playback device can play e.g. switching from Transcode to DirectStream
        if (!deviceTrackingService.GetDeviceStateOrThrow(securityParameters.DeviceId).CanPlay(session.File))
        {
            var transcode = await transcodeRepository.GetAsync(session.File.Path);
            return transcodeCache.GetPlaylistOrThrowAsync(transcode.Id);
        }

        var mimeType = mimeTypeService.GetMimeType(session.File.Path);

        var filePath = rootFileExplorerFolder.GetNodePath(session.AbsolutePath);
        
        return new DirectStreamFile(mimeType)
        {
            FilePath = filePath
        };
    }
}