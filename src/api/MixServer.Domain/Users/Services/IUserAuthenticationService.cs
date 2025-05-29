using MixServer.Domain.Users.Enums;
using MixServer.Domain.Users.Requests;
using MixServer.Domain.Users.Responses;

namespace MixServer.Domain.Users.Services;

public interface IUserAuthenticationService
{
    Task<string> RegisterAsync(string username, ICollection<Role> roles);
    Task RegisterAsync(string username, string temporaryPassword);
    Task ResetPasswordAsync(
        string username,
        string currentPassword,
        string newPassword);
    Task<ITokenRefreshResponse> LoginAsync(IUserLoginRequest request, CancellationToken cancellationToken);
    Task<ITokenRefreshResponse> RefreshAsync(IUserRefreshRequest request, CancellationToken cancellationToken);
}