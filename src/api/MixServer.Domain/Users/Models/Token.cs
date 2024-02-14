namespace MixServer.Domain.Users.Models;

public interface IToken
{
    string AccessToken { get; }
    string RefreshToken { get; }
}

public class Token(string accessToken, string refreshToken) : IToken
{
    public string AccessToken { get; } = accessToken;
    public string RefreshToken { get; } = refreshToken;
}