using MixServer.Domain.Users.Entities;

namespace MixServer.Domain.Users.Responses;

public class UserLoginResponse(UserCredential userCredential, bool passwordResetRequired, List<string> roles) 
    : TokenRefreshResponse(userCredential, passwordResetRequired, roles)
{
}