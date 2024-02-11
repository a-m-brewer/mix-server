namespace MixServer.Domain.Users.Requests;

public interface ITokenRequest
{
    string Audience { get; }
}