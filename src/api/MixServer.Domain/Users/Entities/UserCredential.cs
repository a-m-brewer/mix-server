using MixServer.Domain.Users.Models;

namespace MixServer.Domain.Users.Entities;

public class UserCredential
{
    public Guid Id { get; set; }

    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;

    public string UserId { get; set; } = string.Empty;

    public Device Device { get; set; } = null!;
    public Guid DeviceId { get; set; }

    public void UpdateToken(IToken token)
    {
        AccessToken = token.AccessToken;
        RefreshToken = token.RefreshToken;
    }
}