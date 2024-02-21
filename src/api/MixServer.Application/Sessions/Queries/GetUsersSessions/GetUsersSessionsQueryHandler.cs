using FluentValidation;
using MixServer.Application.Sessions.Responses;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Sessions.Entities;
using MixServer.Domain.Sessions.Services;

namespace MixServer.Application.Sessions.Queries.GetUsersSessions;

public class GetUsersSessionsQueryHandler(
    IConverter<IPlaybackSession, PlaybackSessionDto> playbackSessionConverter,
    ISessionService sessionService,
    IValidator<GetUsersSessionsQuery> validator)
    : IQueryHandler<GetUsersSessionsQuery, GetUsersSessionsResponse>
{
    public async Task<GetUsersSessionsResponse> HandleAsync(GetUsersSessionsQuery request)
    {
        await validator.ValidateAndThrowAsync(request);
        
        var sessions = await sessionService.GetUsersPlaybackSessionHistoryAsync(request.StartIndex, request.PageSize);

        return new GetUsersSessionsResponse
        {
            Sessions = sessions
                .Select(playbackSessionConverter.Convert)
                .ToList()
        };
    }
}