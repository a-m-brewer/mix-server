using MixServer.Domain.Users.Enums;

namespace MixServer.Domain.Users.Services;

public interface IUserRoleService
{
    Task InitializeAsync();
    Task EnsureUserIsInRolesAsync(string userId, ICollection<Role> roles);
}