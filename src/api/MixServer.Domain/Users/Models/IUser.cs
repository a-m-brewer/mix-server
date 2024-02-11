using MixServer.Domain.Users.Enums;

namespace MixServer.Domain.Users.Models;

public interface IUser
{
    string Id { get; }

    string? UserName { get; }

    IList<Role> Roles { get; }

    bool InRole(Role role);
    
    bool IsAdminOrOwner();
}