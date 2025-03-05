using MixServer.Domain.Sessions.Models;

namespace MixServer.Domain.Users.Services;

public interface IStreamKeyService
{
    StreamKey GenerateKey(string value);
}