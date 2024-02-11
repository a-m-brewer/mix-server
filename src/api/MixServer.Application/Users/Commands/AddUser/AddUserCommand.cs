using MixServer.Domain.Users.Enums;

namespace MixServer.Application.Users.Commands.AddUser;

public class AddUserCommand
{
    public string Username { get; set; } = string.Empty;

    public List<Role> Roles { get; set; } = [];
}
