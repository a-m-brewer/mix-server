﻿using FluentValidation;
using MixServer.Domain.Exceptions;
using MixServer.Domain.FileExplorer.Services;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Sessions.Entities;
using MixServer.Domain.Sessions.Services;
using MixServer.Infrastructure.Files.Services;
using MixServer.Infrastructure.Users.Services;

namespace MixServer.Application.Streams.Queries;

public class GetStreamQueryHandler(
    IJwtService jwtService,
    IMimeTypeService mimeTypeService,
    ISessionService sessionService,
    IValidator<GetStreamQuery> validator)
    : IQueryHandler<GetStreamQuery, GetStreamQueryResponse>
{
    public async Task<GetStreamQueryResponse> HandleAsync(GetStreamQuery query)
    {
        await validator.ValidateAndThrowAsync(query);

        var username = await jwtService.GetUsernameFromTokenAsync(query.AccessToken);
        
        var session = await sessionService.GetPlaybackSessionByIdAsync(query.PlaybackSessionId, username);

        if (session.File is null || !session.File.Exists)
        {
            throw new NotFoundException(nameof(PlaybackSession), session.AbsolutePath);
        }

        var mimeType = mimeTypeService.GetMimeType(session.File.AbsolutePath, session.File.Extension);

        var result = new GetStreamQueryResponse
        {
            AbsoluteFilePath = session.AbsolutePath,
            ContentType = mimeType
        };

        return result;
    }
}