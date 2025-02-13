using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MixServer.Domain.FileExplorer.Services.Caching;
using MixServer.Infrastructure.EF.Entities;

namespace MixServer.Infrastructure.Sessions.Services;

public interface ISessionDirectoryCacheInitializationService
{
    Task LoadUsersCurrentPlaybackSessionDirectoriesAsync();
}

public class SessionDirectoryCacheInitializationService(
    IFolderCacheService folderCacheService,
    UserManager<DbUser> userManager) : ISessionDirectoryCacheInitializationService
{
    public async Task LoadUsersCurrentPlaybackSessionDirectoriesAsync()
    {
        var absolutePaths = await userManager.Users
            .Include(i => i.CurrentPlaybackSession)
            .Select(s => s.CurrentPlaybackSession)
            .Where(w => w != null)
            .Distinct()
            .ToListAsync();

        var existingPaths = absolutePaths.Select(s => Path.GetDirectoryName(s!.AbsolutePath))
            .Where(w => !string.IsNullOrWhiteSpace(w) && Directory.Exists(w))
            .Select(s => s!);
        
        var cacheTasks = existingPaths.Select(folderCacheService.GetOrAddAsync);
        
        await Task.WhenAll(cacheTasks);
    }
}