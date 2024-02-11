using MixServer.Domain.Users.Responses;

namespace MixServer.Application.Users.Commands.LoginUser;

public class LoginCommandResponse(ITokenRefreshResponse response) : ITokenRefreshResponse
{
    public bool PasswordResetRequired { get; } = response.PasswordResetRequired;
    public string AccessToken { get; } = response.AccessToken;
    public string RefreshToken { get; } = response.RefreshToken;

    public Guid DeviceId { get; } = response.DeviceId;
    public List<string> Roles { get; set; } = response.Roles;
}