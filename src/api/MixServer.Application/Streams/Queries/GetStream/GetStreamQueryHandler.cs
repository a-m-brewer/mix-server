using FluentValidation;
using MixServer.Domain.Exceptions;
using MixServer.Domain.FileExplorer.Models.Metadata;
using MixServer.Domain.FileExplorer.Services;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Sessions.Entities;
using MixServer.Domain.Sessions.Services;
using MixServer.Domain.Streams.Models;
using MixServer.Domain.Streams.Services;
using MixServer.Infrastructure.Users.Services;

namespace MixServer.Application.Streams.Queries.GetStream;

public class GetStreamQueryHandler(
    IJwtService jwtService,
    IMimeTypeService mimeTypeService,
    ISessionService sessionService,
    ITranscodeService transcodeService,
    IValidator<GetStreamQuery> validator)
    : IQueryHandler<GetStreamQuery, HttpFileInfo>
{
    public async Task<HttpFileInfo> HandleAsync(GetStreamQuery query)
    {
        await validator.ValidateAndThrowAsync(query);

        var username = await jwtService.GetUsernameFromTokenAsync(query.AccessToken);
        
        var session = await sessionService.GetPlaybackSessionByIdAsync(query.PlaybackSessionId, username);

        if (session.File is null || !session.File.Exists)
        {
            throw new NotFoundException(nameof(PlaybackSession), session.AbsolutePath);
        }

        var mimeType = mimeTypeService.GetMimeType(session.File.AbsolutePath, session.File.Extension);

        if (query.Transcode)
        {
            return await transcodeService.StartTranscodeAsync(
                session.File.AbsolutePath,
                (session.File.Metadata as IMediaMetadata)?.Bitrate);
        }

        return new DirectFileInfo(mimeType)
        {
            Path = session.File.AbsolutePath
        };
    }
}