namespace MixServer.Domain.Sessions.Requests;

public interface IUpdateSessionRequest
{
    public TimeSpan CurrentTime { get; }
}