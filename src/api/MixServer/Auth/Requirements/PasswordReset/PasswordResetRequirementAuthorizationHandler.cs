using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using MixServer.Infrastructure.EF.Entities;
using MixServer.Infrastructure.Users.Constants;

namespace MixServer.Auth.Requirements.PasswordReset;

public class PasswordResetRequirementAuthorizationHandler(IServiceProvider serviceProvider)
    : AuthorizationHandler<PasswordResetRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PasswordResetRequirement requirement)
    {
        using var scope = serviceProvider.CreateScope();
        var sp = scope.ServiceProvider;
        var userManager = sp.GetRequiredService<UserManager<DbUser>>();
        
        var userId = context.User.Claims.SingleOrDefault(s => s.Type == CustomClaimTypes.UserId)?.Value;

        if (string.IsNullOrWhiteSpace(userId))
        {
            return;
        }

        var user = await userManager.FindByIdAsync(userId);

        if (user == null  || user.PasswordResetRequired)
        {
            return;
        }
        
        context.Succeed(requirement);
    }
}