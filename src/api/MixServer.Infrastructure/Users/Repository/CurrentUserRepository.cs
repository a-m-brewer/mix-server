using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MixServer.Domain.Exceptions;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Infrastructure.EF;
using MixServer.Infrastructure.EF.Entities;
using MixServer.Infrastructure.EF.Extensions;
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
    Task LoadPlaybackSessionByFileIdAsync(Guid fileId);
    Task LoadPagedPlaybackSessionsAsync(int sessionStartIndex, int sessionPageSize);
    Task LoadFileSortByAbsolutePathAsync(NodePath nodePath);
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
            .Query()
            .IncludeNode()
            .LoadAsync();
    }

    public async Task LoadPlaybackSessionByFileIdAsync(Guid fileId)
    {
        await context.Entry(CurrentUser)
            .Collection(u => u.PlaybackSessions)
            .Query()
            .IncludeNode()
            .Where(w => w.NodeId == fileId)
            .LoadAsync();
    }

    public async Task LoadPagedPlaybackSessionsAsync(int sessionStartIndex, int sessionPageSize)
    {
        await context.Entry(CurrentUser)
            .Collection(u =>u.PlaybackSessions)
            .Query()
            .IncludeNode()
            .OrderByDescending(o => o.LastPlayed)
            .Skip(sessionStartIndex)
            .Take(sessionPageSize)
            .LoadAsync();
    }

    public async Task LoadFileSortByAbsolutePathAsync(NodePath nodePath)
    {
        await context.Entry(CurrentUser)
            .Collection(c => c.FolderSorts)
            .Query()
            .Include(i => i.Node)
            .ThenInclude(t => t!.RootChild)
            .Where(w => 
                w.Node != null &&
                w.Node.RootChild.RelativePath == nodePath.RootPath &&
                w.Node.RelativePath == nodePath.RelativePath)
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