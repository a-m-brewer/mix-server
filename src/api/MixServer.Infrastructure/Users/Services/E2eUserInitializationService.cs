using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MixServer.Domain.Exceptions;
using MixServer.Infrastructure.EF.Entities;
using MixServer.Infrastructure.Users.Settings;

namespace MixServer.Infrastructure.Users.Services;

public interface IE2eUserInitializationService
{
    Task EnsureUserAsync();
}

public class E2eUserInitializationService(
    ILogger<E2eUserInitializationService> logger,
    IOptions<E2eUserSettings> e2eUserSettings,
    UserManager<DbUser> userManager)
    : IE2eUserInitializationService
{
    public async Task EnsureUserAsync()
    {
        foreach (var entry in e2eUserSettings.Value.Users)
        {
            if (string.IsNullOrWhiteSpace(entry.Username) ||
                string.IsNullOrWhiteSpace(entry.Password))
            {
                continue;
            }

            await EnsureSingleUserAsync(entry.Username, entry.Password);
        }
    }

    private async Task EnsureSingleUserAsync(string username, string password)
    {
        var user = await userManager.FindByNameAsync(username);

        if (user == null)
        {
            logger.LogInformation("Creating seeded user: {Username}", username);

            user = new DbUser
            {
                PasswordResetRequired = false,
                UserName = username
            };

            var createResult = await userManager.CreateAsync(user, password);
            ThrowIfInvalidIdentityResult(createResult);
            return;
        }

        if (await userManager.HasPasswordAsync(user))
        {
            var removeResult = await userManager.RemovePasswordAsync(user);
            ThrowIfInvalidIdentityResult(removeResult);
        }

        var addPasswordResult = await userManager.AddPasswordAsync(user, password);
        ThrowIfInvalidIdentityResult(addPasswordResult);

        user.PasswordResetRequired = false;

        var updateResult = await userManager.UpdateAsync(user);
        ThrowIfInvalidIdentityResult(updateResult);
    }

    private static void ThrowIfInvalidIdentityResult(IdentityResult result)
    {
        if (result.Succeeded)
        {
            return;
        }

        var errors = result.Errors
            .ToDictionary(k => k.Code, v => new[] { v.Description });

        throw new InvalidRequestException("Invalid Identity Result", errors);
    }
}
