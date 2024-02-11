using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MixServer.Domain.Users.Services;
using MixServer.Infrastructure.EF;
using MixServer.Infrastructure.Users.Services;
using MixServer.Infrastructure.Users.Settings;

namespace MixServer;

public interface IBootstrapper
{
    Task GoAsync();
}

public class Bootstrapper(
    IWebHostEnvironment environment,
    MixServerDbContext context,
    IOptions<JwtSettings> jwtSettings,
    ILogger<Bootstrapper> logger,
    IFirstUserInitializationService firstUserInitializationService,
    IUserRoleService userRoleService)
    : IBootstrapper
{
    public async Task GoAsync()
    {
        if (environment.IsDevelopment() || context.Database.IsSqlite())
        {
            await context.Database.MigrateAsync();
        }

        await userRoleService.InitializeAsync();
        await firstUserInitializationService.AddFirstUserIfNotExistsAsync();
    }
}