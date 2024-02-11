using MixServer.Application.Sessions.Responses;

namespace MixServer.Application.Sessions.Queries.GetUsersSessions;

public class GetUsersSessionsResponse
{
    public List<PlaybackSessionDto> Sessions { get; set; } = [];
}