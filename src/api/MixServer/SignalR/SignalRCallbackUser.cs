using System.Collections.Concurrent;

namespace MixServer.SignalR;

public class SignalRCallbackUser(SignalRUserId signalRUserId, string accessToken)
{
    private readonly ConcurrentDictionary<SignalRConnectionId, List<SignalRGroup>> _connections = new();

    public string AccessToken { get; set; } = accessToken;
    public SignalRUserId SignalRUserId { get; } = signalRUserId;

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