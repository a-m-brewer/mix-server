using System.ComponentModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using MixServer.Domain.Exceptions;
using MixServer.Domain.Users.Enums;
using MixServer.Domain.Users.Services;
using MixServer.Infrastructure.EF.Entities;

namespace MixServer.Infrastructure.Users.Services;

public interface IIdentityUserRoleService : IUserRoleService
{
    Task<IList<Role>> GetRolesAsync(DbUser user);
}

public class IdentityUserUserRoleService(
    ILogger<IdentityUserUserRoleService> logger,
    RoleManager<IdentityRole> roleManager,
    UserManager<DbUser> userManager) : IIdentityUserRoleService
{
    public async Task InitializeAsync()
    {
        await AddRoleIfNotExistsAsync(Role.Owner);
        await AddRoleIfNotExistsAsync(Role.Administrator);
    }

    public async Task EnsureUserIsInRolesAsync(string userId, ICollection<Role> roles)
    {
        if (roles.Count == 0)
        {
            return;
        }
        
        var user = await userManager.FindByIdAsync(userId) ??
                   throw new NotFoundException(nameof(DbUser), userId);

        var filteredRoles = new List<string>();
        foreach (var role in roles)
        {
            if (!await userManager.IsInRoleAsync(user, role.ToString()))
            {
                filteredRoles.Add(role.ToString());
            }
        }

        var result = await userManager.AddToRolesAsync(user, filteredRoles);

        if (result.Succeeded)
        {
            return;
        }

        var errors = result.Errors
            .ToDictionary(k => k.Code, v => new[] { v.Description });

        throw new InvalidRequestException("Failed to add roles", errors);
    }

    public async Task<IList<Role>> GetRolesAsync(DbUser user)
    {
        var roles = await userManager.GetRolesAsync(user);

        return roles
            .Select(ToEnumRole)
            .ToList();
    }
    
    private async Task AddRoleIfNotExistsAsync(Role role)
    {
        var roleName = role.ToString();
        var roleExists = await roleManager.RoleExistsAsync(roleName);
        logger.LogDebug("Role: {RoleName} exists: {RoleExists}", roleName, roleExists);

        if (roleExists)
        {
            logger.LogDebug("Role: {RoleName} already exists...", roleName);
            return;
        }

        logger.LogInformation("Creating Role: {RoleName}", roleName);
        await roleManager.CreateAsync(new IdentityRole(roleName));
    }
    
    private static Role ToEnumRole(string role)
    {
        return Enum.TryParse(typeof(Role), role, out var enumValue)
            ? (Role)enumValue
            : throw new InvalidEnumArgumentException(nameof(role));
    }
}