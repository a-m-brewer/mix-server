using FluentValidation;
using MixServer.Domain.Exceptions;
using MixServer.Domain.FileExplorer.Services;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Sessions.Entities;
using MixServer.Domain.Sessions.Services;
using MixServer.Infrastructure.Files.Services;
using MixServer.Infrastructure.Users.Services;

namespace MixServer.Application.Streams.Queries;

public class GetStreamQueryHandler : IQueryHandler<GetStreamQuery, GetStreamQueryResponse>
{
    private readonly IJwtService _jwtService;
    private readonly IMimeTypeService _mimeTypeService;
    private readonly ISessionService _sessionService;
    private readonly IValidator<GetStreamQuery> _validator;

    public GetStreamQueryHandler(
        IJwtService jwtService,
        IMimeTypeService mimeTypeService,
        ISessionService sessionService,
        IValidator<GetStreamQuery> validator)
    {
        _jwtService = jwtService;
        _mimeTypeService = mimeTypeService;
        _sessionService = sessionService;
        _validator = validator;
    }
    
    public async Task<GetStreamQueryResponse> HandleAsync(GetStreamQuery query)
    {
        await _validator.ValidateAndThrowAsync(query);

        var claimsIdentity = await _jwtService.GetPrincipalFromTokenAsync(query.AccessToken);

        var username = claimsIdentity.Name ?? throw new UnauthorizedRequestException();
        
        var session = await _sessionService.GetPlaybackSessionByIdAsync(query.PlaybackSessionId, username);

        if (!File.Exists(session.AbsolutePath))
        {
            throw new NotFoundException(nameof(PlaybackSession), session.AbsolutePath);
        }

        var mimeType = _mimeTypeService.GetMimeType(session.AbsolutePath);

        var result = new GetStreamQueryResponse
        {
            AbsoluteFilePath = session.AbsolutePath,
            ContentType = mimeType
        };

        return result;
    }
}