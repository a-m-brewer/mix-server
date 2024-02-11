using MixServer.Domain.Users.Entities;

namespace MixServer.Domain.Users.Responses;

public interface ITokenRefreshResponse
{
    string AccessToken { get; }

    string RefreshToken { get; }
    
    Guid DeviceId { get; }

    List<string> Roles { get; set; }
    
    bool PasswordResetRequired { get; }
}

public class TokenRefreshResponse(
    UserCredential userCredential,
    bool passwordResetRequired,
    List<string> roles) : ITokenRefreshResponse
{
    public string AccessToken { get; } = userCredential.AccessToken;

    public string RefreshToken { get; } = userCredential.RefreshToken;

    public Guid DeviceId { get; } = userCredential.DeviceId;

    public List<string> Roles { get; set; } = roles;
    
    public bool PasswordResetRequired { get; } = passwordResetRequired;
}