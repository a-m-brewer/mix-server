using System.Collections.Concurrent;

namespace MixServer.SignalR;

public class SignalRCallbackUser
{
    private readonly ConcurrentDictionary<SignalRConnectionId, List<SignalRGroup>> _connections = new();

    public SignalRCallbackUser(SignalRUserId signalRUserId, string accessToken)
    {
        SignalRUserId = signalRUserId;
        AccessToken = accessToken;
    }

    public string AccessToken { get; set; }
    public SignalRUserId SignalRUserId { get; }

    public void AddConnection(SignalRConnectionId connectionId, List<SignalRGroup> groups)
    {
        _connections.TryAdd(connectionId, groups);
    }

    public void RemoveConnection(SignalRConnectionId connectionId)
    {
        _connections.TryRemove(connectionId, out _);
    }

    public IReadOnlyList<SignalRConnectionId> GetConnections()
    {
        return _connections.Keys.ToList();
    }

    public IReadOnlyList<SignalRGroup> GetGroups()
    {
        return _connections.Values
            .SelectMany(v => v)
            .Distinct()
            .ToList();
    }

    public IReadOnlyList<SignalRConnectionId> GetConnectionsInGroup(SignalRGroup group)
    {
        return _connections
            .Where(c => c.Value.Contains(group))
            .Select(s => s.Key)
            .ToList();
    }
}