using MixServer.Domain.Persistence;
using MixServer.Domain.Users.Entities;
using MixServer.Domain.Users.Models;

namespace MixServer.Domain.Users.Repositories;

public interface IUserCredentialRepository : ITransientRepository
{
    Task<UserCredential> AddOrUpdateUserCredentialAsync(string userId, Guid deviceId, IToken token);
}