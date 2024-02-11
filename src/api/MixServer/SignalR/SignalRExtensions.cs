using Microsoft.AspNetCore.SignalR;

namespace MixServer.SignalR;

public static class SignalRExtensions
{
    public static ISignalRCallbackClient Clients(this IHubClients<ISignalRCallbackClient> hubClients, IReadOnlyList<SignalRConnectionId> connectionIds)
    {
        return hubClients.Clients(connectionIds.Select(x => x.ToString()).ToList());
    }
}