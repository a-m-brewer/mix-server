namespace MixServer.Domain.Users.Requests;

public interface IUserRefreshRequest : ITokenRequest
{
    string AccessToken { get; }
    string RefreshToken { get; }
    Guid DeviceId { get; }
}