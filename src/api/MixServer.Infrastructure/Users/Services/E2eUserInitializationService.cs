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
        if (string.IsNullOrWhiteSpace(e2eUserSettings.Value.Username) ||
            string.IsNullOrWhiteSpace(e2eUserSettings.Value.Password))
        {
            return;
        }

        var user = await userManager.FindByNameAsync(e2eUserSettings.Value.Username);

        if (user == null)
        {
            logger.LogInformation("Creating E2E user: {Username}", e2eUserSettings.Value.Username);

            user = new DbUser
            {
                PasswordResetRequired = false,
                UserName = e2eUserSettings.Value.Username
            };

            var createResult = await userManager.CreateAsync(user, e2eUserSettings.Value.Password);
            ThrowIfInvalidIdentityResult(createResult);
            return;
        }

        if (await userManager.HasPasswordAsync(user))
        {
            var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
            var resetResult = await userManager.ResetPasswordAsync(user, resetToken, e2eUserSettings.Value.Password);
            ThrowIfInvalidIdentityResult(resetResult);
        }
        else
        {
            var addPasswordResult = await userManager.AddPasswordAsync(user, e2eUserSettings.Value.Password);
            ThrowIfInvalidIdentityResult(addPasswordResult);
        }

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
