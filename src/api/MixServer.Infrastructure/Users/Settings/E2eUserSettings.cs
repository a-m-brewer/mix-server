namespace MixServer.Infrastructure.Users.Settings;

public class E2eUserSettings
{
    public List<E2eUserEntry> Users { get; set; } = [];
}

public class E2eUserEntry
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
