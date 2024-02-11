namespace MixServer.Infrastructure.Users.Settings;

public class JwtSettings
{
    public string SecurityKey { get; set; } = string.Empty;
    public string ValidIssuer { get; set; } = string.Empty;
    public int ExpiryInMinutes { get; set; }
}