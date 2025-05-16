using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MixServer.Domain.Exceptions;
using MixServer.Infrastructure.EF.Entities;
using MixServer.Infrastructure.Users.Repository;
using MixServer.Infrastructure.Users.Services;
using MixServer.Shared.Interfaces;

namespace MixServer.Application.Users.Queries.GetAllUsers;

public class GetAllUsersQueryHandler(
    IConverter<IEnumerable<DbUser>, GetAllUsersResponse> converter,
    ICurrentUserRepository currentUserRepository,
    IIdentityUserRoleService identityUserRoleService,
    UserManager<DbUser> userManager)
    : IQueryHandler<GetAllUsersResponse>
{
    public async Task<GetAllUsersResponse> HandleAsync()
    {
        AssertIsOwnerOrAdmin();

        var users = await userManager.Users.ToListAsync();

        await Task.WhenAll(users.Select(PopulateUserRoleAsync));

        return converter.Convert(users);
    }

    private async Task PopulateUserRoleAsync(DbUser user)
    {
        user.Roles = await identityUserRoleService.GetRolesAsync(user);
    }

    private void AssertIsOwnerOrAdmin()
    {
        if (currentUserRepository.CurrentUser.IsAdminOrOwner())
        {
            return;
        }

        throw new ForbiddenRequestException();
    }
}