using MixServer.Domain.Users.Responses;

namespace MixServer.Application.Users.Commands.RefreshUser;

public class RefreshUserResponse(ITokenRefreshResponse response) : ITokenRefreshResponse
{
    public string AccessToken { get; } = response.AccessToken;
    public string RefreshToken { get; } = response.RefreshToken;

    public Guid DeviceId { get; } = response.DeviceId;
    public List<string> Roles { get; set; } = response.Roles;
    public bool PasswordResetRequired { get; } = response.PasswordResetRequired;
}