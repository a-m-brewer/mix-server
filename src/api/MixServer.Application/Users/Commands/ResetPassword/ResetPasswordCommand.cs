namespace MixServer.Application.Users.Commands.ResetPassword;

public class ResetPasswordCommand
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string NewPasswordConfirmation { get; set; } = string.Empty;
}