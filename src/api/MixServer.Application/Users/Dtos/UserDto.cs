using MixServer.Domain.Users.Enums;

namespace MixServer.Application.Users.Dtos;

public class UserDto
{
    public string UserId { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public List<Role> Roles { get; set; } = [];
}