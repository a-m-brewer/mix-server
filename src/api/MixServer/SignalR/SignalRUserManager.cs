using System.Collections.Concurrent;
using System.Security.Claims;
using MixServer.Domain.Users.Services;
using MixServer.Infrastructure.EF.Entities;
using MixServer.Infrastructure.Users.Constants;
using MixServer.Infrastructure.Users.Services;

namespace MixServer.SignalR;

public interface ISignalRUserManager : IConnectionManager
{
    event EventHandler? UserConnected;

    int UsersConnected { get; }

    Task UserConnectedAsync(ClaimsPrincipal contextUser, SignalRConnectionId signalRConnectionId, string accessToken);
    void UserDisconnected(ClaimsPrincipal contextUser, SignalRConnectionId connectionId);
    
    IReadOnlyList<SignalRConnectionId> GetConnectionsInGroups(params SignalRGroup[] groups);
}

public class SignalRUserManager(IServiceProvider serviceProvider) : ISignalRUserManager
{
    private readonly ConcurrentDictionary<SignalRUserId, SignalRCallbackUser> _users = new();

    public event EventHandler? UserConnected;

    public int UsersConnected => _users.Count;

    public async Task UserConnectedAsync(ClaimsPrincipal contextUser, SignalRConnectionId signalRConnectionId, string accessToken)
    {
        using var scope = serviceProvider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<IIdentityUserAuthenticationService>();

        if (contextUser.Identity == null)
        {
            return;
        }

        var user = await userManager.GetUserByUsernameOrDefaultAsync(contextUser.Identity.Name);

        if (user == null)
        {
            return;
        }
        
        var userId = new SignalRUserId(contextUser.GetNameIdentifier());

        var groups = GetUserGroups(user, contextUser);

        SignalRCallbackUser AddNewUser(SignalRUserId id)
        {
            var callbackUser = new SignalRCallbackUser(id, accessToken);
            AddConnectionAndSetGroups(callbackUser, signalRConnectionId, groups);
            return callbackUser;
        }

        SignalRCallbackUser UpdateExistingUser(SignalRUserId id, SignalRCallbackUser existingUser)
        {
            existingUser.AccessToken = accessToken;
            AddConnectionAndSetGroups(existingUser, signalRConnectionId, groups);
            return existingUser;
        }

        _users.AddOrUpdate(userId, AddNewUser, UpdateExistingUser);
        UserConnected?.Invoke(this, EventArgs.Empty);
    }

    public void UserDisconnected(ClaimsPrincipal contextUser, SignalRConnectionId connectionId)
    {
        RemoveConnection(contextUser, connectionId);
    }

    public IReadOnlyList<SignalRConnectionId> GetConnectionsInGroups(params SignalRGroup[] groups)
    {
        var usersInGroup = _users.Values
            .Select(u => groups.Select(u.GetConnectionsInGroup).SelectMany(g => g))
            .SelectMany(c => c)
            .ToList();

        return usersInGroup;
    }

    public bool DeviceConnected(Guid deviceId)
    {
        var connections = GetConnectionsInGroups(new SignalRGroup(deviceId.ToString()));

        return connections.Any();
    }
    
    private void RemoveConnection(ClaimsPrincipal contextUser, SignalRConnectionId connectionId)
    {
        var userId = new SignalRUserId(contextUser.GetNameIdentifier());
        if (!_users.TryGetValue(userId, out var callbackUser))
        {
            return;
        }

        callbackUser.RemoveConnection(connectionId);
        if (!callbackUser.GetConnections().Any())
        {
            _users.TryRemove(userId, out _);
        }
    }

    private List<SignalRGroup> GetUserGroups(DbUser user, ClaimsPrincipal claimsPrincipal)
    {
        var groups = new List<SignalRGroup>
        {
            new(user.Id)
        };

        if (claimsPrincipal.HasClaim(c => c.Type == CustomClaimTypes.DeviceId))
        {
            var deviceId = claimsPrincipal.Claims
                .Single(s => s.Type == CustomClaimTypes.DeviceId)
                .Value;

            groups.Add(new SignalRGroup(deviceId));
        }

        groups.AddRange(user.Roles.Select(role => new SignalRGroup(role.ToString())));

        return groups;
    }
    
    private static void AddConnectionAndSetGroups(SignalRCallbackUser callbackUser,
        SignalRConnectionId connectionId,
        List<SignalRGroup> groups)
    {
        callbackUser.AddConnection(connectionId, groups);
    }
}