namespace MixServer.Domain.Users.Requests;

public interface IUserLoginRequest : ITokenRequest
{
    string Username { get; }
    string Password { get; }
    Guid? DeviceId { get; }
}