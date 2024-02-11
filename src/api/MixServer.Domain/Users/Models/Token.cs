namespace MixServer.Domain.Users.Models;

public interface IToken
{
    string AccessToken { get; }
    string RefreshToken { get; }
}

public class Token : IToken
{
    public Token(string accessToken, string refreshToken)
    {
        AccessToken = accessToken;
        RefreshToken = refreshToken;
    }

    public string AccessToken { get; }
    public string RefreshToken { get; }
}