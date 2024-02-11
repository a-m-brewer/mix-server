using MixServer.Domain.Users.Requests;

namespace MixServer.Application.Users.Commands.RefreshUser;

public class RefreshUserCommand : TokenCommand, IUserRefreshRequest
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public Guid DeviceId { get; set; } = Guid.Empty;
}