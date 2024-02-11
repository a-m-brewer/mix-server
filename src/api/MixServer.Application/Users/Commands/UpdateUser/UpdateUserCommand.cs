using System.Runtime.Serialization;
using MixServer.Domain.Users.Enums;

namespace MixServer.Application.Users.Commands.UpdateUser;

public class UpdateUserCommand
{
    [IgnoreDataMember]
    public string UserId { get; set; } = string.Empty;

    public List<Role>? Roles { get; set; }
}