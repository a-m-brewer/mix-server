using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
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
    void SetUserId(string userId);
    Task<DbUser> GetCurrentUserAsync();
    Task LoadCurrentPlaybackSessionAsync();
    Task LoadPlaybackSessionByFileIdAsync(Guid fileId);
    Task LoadPagedPlaybackSessionsAsync(int sessionStartIndex, int sessionPageSize);
    Task LoadFileSortByAbsolutePathAsync(NodePath nodePath);
    Task LoadAllDevicesAsync();
    Task LoadDeviceByIdAsync(Guid deviceId);
}

public class CurrentUserRepository : ICurrentUserRepository
{
    private readonly Lazy<Task<DbUser?>> _currentUserLazy;
    private readonly IHttpContextAccessor _contextAccessor;
    private readonly MixServerDbContext _context;
    private readonly IIdentityUserRoleService _identityUserRoleService;
    private readonly UserManager<DbUser> _userManager;
    private string? _userIdOverride;

    public CurrentUserRepository(IHttpContextAccessor contextAccessor,
        MixServerDbContext context,
        IIdentityUserRoleService identityUserRoleService,
        UserManager<DbUser> userManager)
    {
        _currentUserLazy = new Lazy<Task<DbUser?>>(LoadUserAsync);
        _contextAccessor = contextAccessor;
        _context = context;
        _identityUserRoleService = identityUserRoleService;
        _userManager = userManager;
    }

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

    public void SetUserId(string userId)
    {
        _userIdOverride = userId;
    }

    public async Task<DbUser> GetCurrentUserAsync()
    {
        return await _currentUserLazy.Value ?? throw new UnauthorizedRequestException();
    }

    private async Task<DbUser?> LoadUserAsync()
    {
        return await LoadUserInternalAsync(InternalUserId);
    }
    
    private async Task<DbUser?> LoadUserInternalAsync(string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
        {
            return null;
        }

        user.Roles = await _identityUserRoleService.GetRolesAsync(user);

        return user;
    }

    public async Task LoadCurrentPlaybackSessionAsync()
    {
        await (await CurrentUserEntry())
            .Reference(u => u.CurrentPlaybackSession)
            .Query()
            .IncludeNode()
            .LoadAsync();
    }

    public async Task LoadPlaybackSessionByFileIdAsync(Guid fileId)
    {
        await (await CurrentUserEntry())
            .Collection(u => u.PlaybackSessions)
            .Query()
            .IncludeNode()
            .Where(w => w.NodeId == fileId)
            .LoadAsync();
    }

    public async Task LoadPagedPlaybackSessionsAsync(int sessionStartIndex, int sessionPageSize)
    {
        await (await CurrentUserEntry())
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
        await (await CurrentUserEntry())
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
        await (await CurrentUserEntry())
            .Collection(u => u.Devices)
            .Query()
            .OrderByDescending(o => o.LastSeen)
            .LoadAsync();
    }

    public async Task LoadDeviceByIdAsync(Guid deviceId)
    {
        await (await CurrentUserEntry())
            .Collection(c => c.Devices)
            .Query()
            .Where(w => w.Id == deviceId)
            .LoadAsync();
    }

    private ClaimsPrincipal? User => _contextAccessor.HttpContext?.User;

    private string? InternalUserId =>
        string.IsNullOrWhiteSpace(_userIdOverride)
            ? User?.Claims.SingleOrDefault(s => s.Type == CustomClaimTypes.UserId)?.Value
            : _userIdOverride;

    private async Task<EntityEntry<DbUser>> CurrentUserEntry()
    {
        var user = await _currentUserLazy.Value;
        if (user == null)
        {
            throw new UnauthorizedRequestException();
        }
        
        return _context.Entry(user);
    }
}