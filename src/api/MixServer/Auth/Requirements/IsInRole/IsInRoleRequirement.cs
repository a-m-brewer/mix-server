using Microsoft.AspNetCore.Authorization;
using MixServer.Domain.Users.Enums;

namespace MixServer.Auth.Requirements.IsInRole;

public class IsInRoleRequirement(ICollection<Role> roles) : IAuthorizationRequirement
{
    public ICollection<Role> Roles { get; } = roles;
}