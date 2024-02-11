using MixServer.Domain.Users.Requests;

namespace MixServer.Application.Users.Commands.LoginUser;

public class LoginUserCommand : TokenCommand, IUserLoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    public Guid? DeviceId { get; set; } = null;
}