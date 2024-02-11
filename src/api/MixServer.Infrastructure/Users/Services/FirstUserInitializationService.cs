using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MixServer.Domain.Users.Enums;
using MixServer.Domain.Users.Services;
using MixServer.Infrastructure.EF.Entities;
using MixServer.Infrastructure.Users.Settings;

namespace MixServer.Infrastructure.Users.Services;

public interface IFirstUserInitializationService
{
    Task AddFirstUserIfNotExistsAsync();
}

public class FirstUserInitializationService(
    ILogger<FirstUserInitializationService> logger,
    IUserAuthenticationService userAuthenticationService,
    IUserRoleService userRoleService,
    IOptions<InitialUserSettings> initialUserSettings,
    UserManager<DbUser> userManager) : IFirstUserInitializationService
{
    public async Task AddFirstUserIfNotExistsAsync()
    {
        var userCount = await userManager.Users.CountAsync();

        if (userCount > 1)
        {
            logger.LogInformation("More than 1 user exists system initialized...");
            return;
        }

        var user = await userManager.Users
            .OrderBy(o => o.Id)
            .FirstOrDefaultAsync();

        if (user == null)
        {
            logger.LogInformation("Initial User does not exist creating user");
            await userAuthenticationService.RegisterAsync(
                initialUserSettings.Value.Username,
                initialUserSettings.Value.TemporaryPassword);
            
            user = await userManager.Users.FirstAsync(f => f.UserName == initialUserSettings.Value.Username);
        }

        await userRoleService.EnsureUserIsInRolesAsync(user.Id, new []{ Role.Owner, Role.Administrator });
    }
}