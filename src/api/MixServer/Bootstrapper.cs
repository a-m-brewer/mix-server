﻿using Microsoft.EntityFrameworkCore;
using MixServer.Domain.FileExplorer.Services;
using MixServer.Domain.Streams.Caches;
using MixServer.Domain.Streams.Services;
using MixServer.Domain.Users.Services;
using MixServer.Infrastructure.EF;
using MixServer.Infrastructure.Sessions.Services;
using MixServer.Infrastructure.Users.Services;
using MixServer.Services;

namespace MixServer;

public interface IBootstrapper
{
    Task GoAsync();
}

public class Bootstrapper(
    AbsolutePathMigrationService absolutePathMigrationService,
    IWebHostEnvironment environment,
    MixServerDbContext context,
    IFileNotificationService fileNotificationService,
    IFirstUserInitializationService firstUserInitializationService,
    ITranscodeCache transcodeCache,
    IUserRoleService userRoleService)
    : IBootstrapper
{
    public async Task GoAsync()
    {
        if (environment.IsDevelopment() || context.Database.IsSqlite())
        {
            await context.Database.MigrateAsync();
        }
        
        await absolutePathMigrationService.MigrateAsync();

        await userRoleService.InitializeAsync();
        await firstUserInitializationService.AddFirstUserIfNotExistsAsync();
        fileNotificationService.Initialize();
        
        await transcodeCache.InitializeAsync();
    }
}