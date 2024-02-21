using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MixServer.Domain.Exceptions;
using MixServer.Domain.Persistence;
using MixServer.Domain.Users.Enums;
using MixServer.Domain.Users.Models;
using MixServer.Domain.Users.Repositories;
using MixServer.Domain.Users.Requests;
using MixServer.Domain.Users.Responses;
using MixServer.Domain.Users.Services;
using MixServer.Infrastructure.EF.Entities;
using MixServer.Infrastructure.Users.Constants;

namespace MixServer.Infrastructure.Users.Services;

public interface IIdentityUserAuthenticationService : IUserAuthenticationService
{
    Task<DbUser> GetUserByIdOrThrowAsync(string userId);
    Task<DbUser?> GetUserByUsernameOrDefaultAsync(string? username);
    Task DeleteUserAsync(DbUser user);
}

public class IdentityUserAuthenticationService(
    IDeviceService deviceService,
    IJwtService jwtService,
    IPasswordGeneratorService passwordGeneratorService,
    IUnitOfWork unitOfWork,
    IUserCredentialRepository userCredentialRepository,
    IIdentityUserRoleService userRoleService,
    UserManager<DbUser> userManager)
    : IIdentityUserAuthenticationService
{
    public async Task<string> RegisterAsync(string username, ICollection<Role> roles)
    {
        var temporaryPassword = passwordGeneratorService.Generate();
        
        var user = await RegisterInternalAsync(username, temporaryPassword);
        
        await userRoleService.EnsureUserIsInRolesAsync(user.Id, roles);
        
        unitOfWork.InvokeCallbackOnSaved(c => c.UserAdded(user));

        return temporaryPassword;
    }

    public async Task RegisterAsync(string username, string temporaryPassword)
    {
        var user = await RegisterInternalAsync(username, temporaryPassword);
        
        unitOfWork.InvokeCallbackOnSaved(c => c.UserAdded(user));
    }

    public async Task ResetPasswordAsync(
        string username,
        string currentPassword,
        string newPassword)
    {
        var user = await GetUserOrThrowAsync(username);

        var identityResult = await userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        
        ThrowIfInvalidIdentityResult(identityResult);

        user.PasswordResetRequired = false;
    }

    private async Task<DbUser> RegisterInternalAsync(string username, string temporaryPassword)
    {
        var user = new DbUser
        {
            UserName = username,
            PasswordResetRequired = true
        };
        
        var result = await userManager.CreateAsync(user, temporaryPassword);

        ThrowIfInvalidIdentityResult(result);

        return user;
    }

    public async Task<ITokenRefreshResponse> LoginAsync(IUserLoginRequest request)
    {
        var user = await userManager.Users
            .Include(i => i.Devices)
            .SingleOrDefaultAsync(s => s.UserName == request.Username);

        if (user?.UserName == null || !await userManager.CheckPasswordAsync(user,  request.Password))
        {
            throw new UnauthorizedRequestException();
        }

        var device = await deviceService.GetOrAddAsync(request.DeviceId);

        if (user.Devices.All(a => a.Id != device.Id))
        {
            user.Devices.Add(device);
        }
        
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.UserName),
            new(CustomClaimTypes.UserId, user.Id),
            new(CustomClaimTypes.DeviceId, device.Id.ToString())
        };

        var roles = await userManager.GetRolesAsync(user);

        var newToken = GenerateTokens(request.Audience, claims);

        var userCredential = await userCredentialRepository.AddOrUpdateUserCredentialAsync(user.Id, device.Id, newToken);

        deviceService.UpdateDevice(device);

        await unitOfWork.SaveChangesAsync();

        return new UserLoginResponse(userCredential, user.PasswordResetRequired, roles.ToList());
    }

    public async Task<ITokenRefreshResponse> RefreshAsync(IUserRefreshRequest request)
    {
        var claimsIdentity = await jwtService.GetPrincipalFromTokenAsync(request.AccessToken);
        var username = claimsIdentity.Name ?? throw new UnauthorizedRequestException();

        var user = await GetUserOrThrowAsync(username);

        var device = await deviceService.SingleOrDefaultAsync(request.DeviceId);

        if (device == null)
        {
            throw new UnauthorizedRequestException();
        }
        
        var roles = await userManager.GetRolesAsync(user);
        
        var newToken = GenerateTokens(request.Audience,  claimsIdentity.Claims);

        var userCredential = await userCredentialRepository.AddOrUpdateUserCredentialAsync(user.Id, device.Id, newToken);

        deviceService.UpdateDevice(device);
        
        await unitOfWork.SaveChangesAsync();

        return new TokenRefreshResponse(userCredential, user.PasswordResetRequired, roles.ToList());
    }

    public async Task<DbUser> GetUserByIdOrThrowAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);

        if (user == null)
        {
            throw new UnauthorizedRequestException();
        }
        
        user.Roles = await userRoleService.GetRolesAsync(user);
        
        return user;
    }

    public async Task<DbUser?> GetUserByUsernameOrDefaultAsync(string? username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return null;
        }
        
        var user = await userManager.FindByNameAsync(username);
        
        if (user == null)
        {
            return null;
        }
        
        user.Roles = await userRoleService.GetRolesAsync(user);
        
        return user;
    }

    public async Task DeleteUserAsync(DbUser user)
    {
        await userManager.DeleteAsync(user);
        
        unitOfWork.InvokeCallbackOnSaved(c => c.UserDeleted(user.Id));
    }

    private async Task<DbUser> GetUserOrThrowAsync(string username)
    {
        var user = await userManager.FindByNameAsync(username);

        if (user == null)
        {
            throw new UnauthorizedRequestException();
        }

        return user;
    }

    private IToken GenerateTokens(
        string audience,
        IEnumerable<Claim> claims)
    {
        var signingCredentials = jwtService.GetSigningCredentials();
        var tokenOptions = jwtService.GenerateTokenOptions(audience, signingCredentials, claims);
        var token = new JwtSecurityTokenHandler().WriteToken(tokenOptions);

        var refreshToken = jwtService.GenerateRefreshToken();

        return new Token(token, refreshToken);
    }

    private static void ThrowIfInvalidIdentityResult(IdentityResult result)
    {
        if (result.Succeeded)
        {
            return;
        }

        var errors = result.Errors
            .ToDictionary(k => k.Code, v => new[] { v.Description });

        throw new InvalidRequestException("Invalid Identity Result", errors);
    }
}