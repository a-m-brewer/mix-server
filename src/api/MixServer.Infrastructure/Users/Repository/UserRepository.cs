using Microsoft.AspNetCore.Identity;
using MixServer.Domain.Exceptions;
using MixServer.Infrastructure.EF.Entities;

namespace MixServer.Infrastructure.Users.Repository;

public interface IUserRepository
{
    Task<DbUser> GetUserAsync(string username);
}

public class UserRepository(UserManager<DbUser> userManager) : IUserRepository
{
    public async Task<DbUser> GetUserAsync(string username)
    {
        return await userManager.FindByNameAsync(username) ?? throw new UnauthorizedRequestException();
    }
}