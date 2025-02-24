using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MixServer.Domain.Exceptions;
using MixServer.Infrastructure.EF;
using MixServer.Infrastructure.EF.Entities;
using MixServer.Infrastructure.Users.Constants;
using MixServer.Infrastructure.Users.Services;

namespace MixServer.Infrastructure.Users.Repository;

public interface ICurrentUserRepository
{
    string CurrentUserId { get; }
    DbUser CurrentUser { get; }
    bool CurrentUserLoaded { get; }
    Task LoadUserAsync();
    Task LoadUserAsync(string userId);
    Task LoadCurrentPlaybackSessionAsync();
    Task LoadAllPlaybackSessionsAsync();
    Task LoadPlaybackSessionAsync(string absolutePath);
    Task LoadPagedPlaybackSessionsAsync(int sessionStartIndex, int sessionPageSize);
    Task LoadAllFileSortsAsync();
    Task LoadFileSortByAbsolutePathAsync(string absoluteFolderPath);
    Task LoadAllDevicesAsync();
    Task LoadDeviceByIdAsync(Guid deviceId);
}

public class CurrentUserRepository(
    IHttpContextAccessor contextAccessor,
    MixServerDbContext context,
    IIdentityUserRoleService identityUserRoleService,
    UserManager<DbUser> userManager)
    : ICurrentUserRepository
{
    private DbUser? _currentUser;

    public string CurrentUserId
    {
        get
        {
            var internalUserId = InternalUserId;

            if (string.IsNullOrWhiteSpace(internalUserId))
            {
                throw new UnauthorizedRequestException();
            }

            return internalUserId;
        }
    }

    public DbUser CurrentUser => _currentUser ?? throw new UnauthorizedRequestException();
    public bool CurrentUserLoaded => _currentUser != null;

    public async Task LoadUserAsync()
    {
        await LoadUserAsync(InternalUserId);
    }

    public async Task LoadUserAsync(string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            _currentUser = null;
            return;
        }

        if (_currentUser != null && _currentUser.Id == userId)
        {
            return;
        }

        _currentUser = await userManager.FindByIdAsync(userId);

        if (_currentUser == null)
        {
            return;
        }

        _currentUser.Roles = await identityUserRoleService.GetRolesAsync(_currentUser);
    }

    public async Task LoadCurrentPlaybackSessionAsync()
    {
        await context.Entry(CurrentUser)
            .Reference(u => u.CurrentPlaybackSession)
            .LoadAsync();
    }

    public async Task LoadAllPlaybackSessionsAsync()
    {
        await context.Entry(CurrentUser)
            .Collection(u => u.PlaybackSessions)
            .Query()
            .OrderByDescending(o => o.LastPlayed)
            .LoadAsync();
    }

    public async Task LoadPlaybackSessionAsync(string absolutePath)
    {
        await context.Entry(CurrentUser)
            .Collection(u => u.PlaybackSessions)
            .Query()
            .Where(w => w.AbsolutePath == absolutePath)
            .LoadAsync();
    }

    public async Task LoadPagedPlaybackSessionsAsync(int sessionStartIndex, int sessionPageSize)
    {
        await context.Entry(CurrentUser)
            .Collection(u =>u.PlaybackSessions)
            .Query()
            .OrderByDescending(o => o.LastPlayed)
            .Skip(sessionStartIndex)
            .Take(sessionPageSize)
            .LoadAsync();
    }

    public async Task LoadAllFileSortsAsync()
    {
        await context.Entry(CurrentUser)
            .Collection(u => u.FolderSorts)
            .LoadAsync();
    }

    public async Task LoadFileSortByAbsolutePathAsync(string absoluteFolderPath)
    {
        await context.Entry(CurrentUser)
            .Collection(c => c.FolderSorts)
            .Query()
            .Where(w => w.AbsoluteFolderPath == absoluteFolderPath)
            .LoadAsync();
    }

    public async Task LoadAllDevicesAsync()
    {
        await context.Entry(CurrentUser)
            .Collection(u => u.Devices)
            .Query()
            .OrderByDescending(o => o.LastSeen)
            .LoadAsync();
    }

    public async Task LoadDeviceByIdAsync(Guid deviceId)
    {
        await context.Entry(CurrentUser)
            .Collection(c => c.Devices)
            .Query()
            .Where(w => w.Id == deviceId)
            .LoadAsync();
    }

    private ClaimsPrincipal? User => contextAccessor.HttpContext?.User;
    
    private string? InternalUserId => User?.Claims.SingleOrDefault(s => s.Type == CustomClaimTypes.UserId)?.Value;
}