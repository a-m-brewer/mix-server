using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MixServer.Domain.Exceptions;
using MixServer.Domain.Interfaces;
using MixServer.Infrastructure.EF.Entities;
using MixServer.Infrastructure.Users.Repository;
using MixServer.Infrastructure.Users.Services;

namespace MixServer.Application.Users.Queries.GetAllUsers;

public class GetAllUsersQueryHandler(
    IConverter<IEnumerable<DbUser>, GetAllUsersResponse> converter,
    ICurrentUserRepository currentUserRepository,
    IIdentityUserRoleService identityUserRoleService,
    UserManager<DbUser> userManager)
    : IQueryHandler<GetAllUsersResponse>
{
    public async Task<GetAllUsersResponse> HandleAsync(CancellationToken cancellationToken = default)
    {
        await AssertIsOwnerOrAdminAsync();

        var users = await userManager.Users.ToListAsync(cancellationToken);

        await Task.WhenAll(users.Select(PopulateUserRoleAsync));

        return converter.Convert(users);
    }

    private async Task PopulateUserRoleAsync(DbUser user)
    {
        user.Roles = await identityUserRoleService.GetRolesAsync(user);
    }

    private async Task AssertIsOwnerOrAdminAsync()
    {
        if ((await currentUserRepository.GetCurrentUserAsync()).IsAdminOrOwner())
        {
            return;
        }

        throw new ForbiddenRequestException();
    }
}