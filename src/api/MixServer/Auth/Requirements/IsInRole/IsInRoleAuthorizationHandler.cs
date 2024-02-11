using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using MixServer.Infrastructure.EF.Entities;
using MixServer.Infrastructure.Users.Constants;
using MixServer.Infrastructure.Users.Services;

namespace MixServer.Auth.Requirements.IsInRole;

public class IsInRoleAuthorizationHandler(IServiceProvider serviceProvider) : AuthorizationHandler<IsInRoleRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, IsInRoleRequirement requirement)
    {
        using var scope = serviceProvider.CreateScope();
        var sp = scope.ServiceProvider;
        var userManager = sp.GetRequiredService<UserManager<DbUser>>();
        var roleService = sp.GetRequiredService<IIdentityUserRoleService>();
        
        var userId = context.User.Claims.SingleOrDefault(s => s.Type == CustomClaimTypes.UserId)?.Value;

        if (string.IsNullOrWhiteSpace(userId))
        {
            return;
        }

        var user = await userManager.FindByIdAsync(userId);

        if (user == null)
        {
            return;
        }

        user.Roles = await roleService.GetRolesAsync(user);

        if (requirement.Roles.Any(role => user.InRole(role)))
        {
            context.Succeed(requirement);
        }
    }
}