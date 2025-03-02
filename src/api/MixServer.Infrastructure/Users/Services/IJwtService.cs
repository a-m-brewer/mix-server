using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using MixServer.Domain.Users.Services;

namespace MixServer.Infrastructure.Users.Services;

public interface IJwtService : IStreamKeyService
{
    SigningCredentials GetSigningCredentials();

    JwtSecurityToken GenerateTokenOptions(
        string audience,
        SigningCredentials signingCredentials,
        IEnumerable<Claim> claims);

    string GenerateRefreshToken();
    
    Task<(string Username, IReadOnlyCollection<Claim> Claims)> GetUsernameAndClaimsFromTokenAsync(string token);
    
    Task<string> GetUsernameFromTokenAsync(string token);

    Task<IReadOnlyCollection<Claim>> GetClaimsFromTokenAsync(string token);
    void ValidateKeyOrThrow(string key, string signedKey, double expires);
}