namespace MixServer.Application.Sessions.Queries.GetUsersSessions;

public class GetUsersSessionsQuery
{
    public int StartIndex { get; set; } = 0;

    public int EndIndex { get; set; } = 25;
}