using FluentValidation;
using MixServer.Application.Sessions.Responses;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Sessions.Entities;
using MixServer.Domain.Sessions.Services;

namespace MixServer.Application.Sessions.Queries.GetUsersSessions;

public class GetUsersSessionsQueryHandler : IQueryHandler<GetUsersSessionsQuery, GetUsersSessionsResponse>
{
    private readonly IConverter<IPlaybackSession, PlaybackSessionDto> _playbackSessionConverter;
    private readonly ISessionService _sessionService;
    private readonly IValidator<GetUsersSessionsQuery> _validator;

    public GetUsersSessionsQueryHandler(
        IConverter<IPlaybackSession, PlaybackSessionDto> playbackSessionConverter,
        ISessionService sessionService,
        IValidator<GetUsersSessionsQuery> validator)
    {
        _playbackSessionConverter = playbackSessionConverter;
        _sessionService = sessionService;
        _validator = validator;
    }
    
    public async Task<GetUsersSessionsResponse> HandleAsync(GetUsersSessionsQuery request)
    {
        await _validator.ValidateAndThrowAsync(request);
        
        var sessions = await _sessionService.GetUsersPlaybackSessionHistoryAsync(request.StartIndex, request.PageSize);

        return new GetUsersSessionsResponse
        {
            Sessions = sessions
                .Select(_playbackSessionConverter.Convert)
                .ToList()
        };
    }
}