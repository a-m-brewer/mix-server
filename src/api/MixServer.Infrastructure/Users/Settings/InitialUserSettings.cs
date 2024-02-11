using MixServer.Infrastructure.Users.Constants;

namespace MixServer.Infrastructure.Users.Settings;

public class InitialUserSettings
{
    public string Username { get; set; } = UserDefaults.DefaultUsername;
    public string TemporaryPassword { get; set; } = UserDefaults.DefaultTemporaryPassword;
}