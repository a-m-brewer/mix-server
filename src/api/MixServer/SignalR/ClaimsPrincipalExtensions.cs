using System.Security.Claims;

namespace MixServer.SignalR;

public static class ClaimsPrincipalExtensions
{
    public static string GetNameIdentifier(this ClaimsPrincipal claimsPrincipal)
    {
        var claim = claimsPrincipal.Claims.Single(c => c.Type == ClaimTypes.Name);
        return claim.Value;
    }
}