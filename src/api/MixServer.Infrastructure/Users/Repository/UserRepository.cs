using Microsoft.AspNetCore.Identity;
using MixServer.Domain.Exceptions;
using MixServer.Infrastructure.EF.Entities;

namespace MixServer.Infrastructure.Users.Repository;

public interface IUserRepository
{
    Task<DbUser> GetUserAsync(string username);
}

public class UserRepository : IUserRepository
{
    private readonly UserManager<DbUser> _userManager;

    public UserRepository(UserManager<DbUser> userManager)
    {
        _userManager = userManager;
    }
    
    public async Task<DbUser> GetUserAsync(string username)
    {
        return await _userManager.FindByNameAsync(username) ?? throw new UnauthorizedRequestException();
    }
}