using Microsoft.EntityFrameworkCore;
using MixServer.Domain.Users.Entities;
using MixServer.Domain.Users.Models;
using MixServer.Domain.Users.Repositories;

namespace MixServer.Infrastructure.EF.Repositories;

public class EfUserCredentialRepository(MixServerDbContext context) : IUserCredentialRepository
{
    public async Task<UserCredential> AddOrUpdateUserCredentialAsync(
        string userId,
        Guid deviceId,
        IToken token,
        CancellationToken cancellationToken)
    {
        var credential = await context.UserCredentials.SingleOrDefaultAsync(s => s.UserId == userId && s.DeviceId == deviceId, cancellationToken: cancellationToken);

        if (credential == null)
        {
            credential = new UserCredential
            {
                Id = Guid.NewGuid(),
                DeviceId = deviceId,
                UserId = userId,
                AccessToken = token.AccessToken,
                RefreshToken = token.RefreshToken
            };

            await context.UserCredentials.AddAsync(credential, cancellationToken);
        }
        else
        {
            credential.UpdateToken(token);
        }

        return credential;
    }
}