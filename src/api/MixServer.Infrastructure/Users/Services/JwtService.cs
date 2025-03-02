using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Web;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MixServer.Domain.Exceptions;
using MixServer.Domain.Sessions.Models;
using MixServer.Infrastructure.Server.Settings;
using MixServer.Infrastructure.Users.Settings;

namespace MixServer.Infrastructure.Users.Services;

public class JwtService(
    IOptions<HostSettings> hostSettings,
    IOptions<JwtSettings> jwtSettings)
    : IJwtService
{
    public SigningCredentials GetSigningCredentials()
    {
        var secret = GetPrivateKey();

        return new SigningCredentials(secret, SecurityAlgorithms.HmacSha256);
    }

    public StreamKey GenerateKey(string value) => GenerateKey(value, GetUnixEpoch(DateTime.UtcNow.AddDays(1)));

    public JwtSecurityToken GenerateTokenOptions(
        string audience,
        SigningCredentials signingCredentials,
        IEnumerable<Claim> claims)
    {
        if (!hostSettings.Value.ValidAuthorities.Contains(audience))
        {
            throw new UnauthorizedRequestException();
        }
        
        var tokenOptions = new JwtSecurityToken(
            issuer: jwtSettings.Value.ValidIssuer,
            audience: audience,
            claims: claims,
            expires: DateTime.Now.AddMinutes(Convert.ToDouble(jwtSettings.Value.ExpiryInMinutes)),
            signingCredentials: signingCredentials);

        return tokenOptions;
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public async Task<(string Username, IReadOnlyCollection<Claim> Claims)> GetUsernameAndClaimsFromTokenAsync(string token)
    {
        var claimsPrincipal = await GetPrincipalFromTokenAsync(token);
        
        var username = GetUsernameFromToken(claimsPrincipal);
        
        var claims = GetClaimsFromToken(claimsPrincipal);
        
        return (username, claims);
    }

    public async Task<string> GetUsernameFromTokenAsync(string token)
    {
        var claimsPrincipal = await GetPrincipalFromTokenAsync(token);
        
        return GetUsernameFromToken(claimsPrincipal);
    }

    private string GetUsernameFromToken(IIdentity identity)
    {
        var username = identity.Name;

        if (string.IsNullOrWhiteSpace(username))
        {
            throw new UnauthorizedRequestException();
        }
        
        return username;
    }

    public async Task<IReadOnlyCollection<Claim>> GetClaimsFromTokenAsync(string token)
    {
        var claimsPrincipal = await GetPrincipalFromTokenAsync(token);
        
        return GetClaimsFromToken(claimsPrincipal);
    }

    public void ValidateKeyOrThrow(string key, string signedKey, double expires)
    {
        var now = GetUnixEpoch(DateTime.UtcNow);
        if (now > expires)
        {
            throw new UnauthorizedRequestException();
        }
        
        var expectedKey = GenerateKey(key, expires).Key;
        if (expectedKey != signedKey)
        {
            throw new UnauthorizedRequestException();
        }
    }

    private IReadOnlyCollection<Claim> GetClaimsFromToken(ClaimsIdentity claimsIdentity)
    {
        return claimsIdentity.Claims
            .Where(w => w.Type != "aud")
            .ToList();
    }

    private async Task<ClaimsIdentity> GetPrincipalFromTokenAsync(string token)
    {
        var tokenValidationParameters = GetTokenValidationParameters(validateLifetime: false);

        var tokenHandler = new JwtSecurityTokenHandler();

        var validationResult = await tokenHandler.ValidateTokenAsync(token, tokenValidationParameters);

        if (!validationResult.IsValid)
        {
            throw new UnauthorizedRequestException("Failed to Validate Token", validationResult.Exception);
        }

        var securityToken = validationResult.SecurityToken;

        if (securityToken is not JwtSecurityToken jwtSecurityToken ||
            !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
        {
            throw new UnauthorizedRequestException();
        }
        
        return validationResult.ClaimsIdentity;
    }

    public static TokenValidationParameters GetTokenValidationParameters(
        HostSettings hostSettings,
        JwtSettings jwtSettings,
        bool validateLifetime = true)
    {
        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = validateLifetime,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.ValidIssuer,
            ValidAudiences = hostSettings.ValidAuthorities,
            IssuerSigningKey = GetPrivateKey(jwtSettings.SecurityKey)
        };
    }

    private TokenValidationParameters GetTokenValidationParameters(bool validateLifetime = true)
        => GetTokenValidationParameters(
            hostSettings.Value,
            jwtSettings.Value,
            validateLifetime);
    
    private SymmetricSecurityKey GetPrivateKey()
    {
        var key = Encoding.UTF8.GetBytes(jwtSettings.Value.SecurityKey);
        var secret = new SymmetricSecurityKey(key);

        return secret;
    }
    
    private static SymmetricSecurityKey GetPrivateKey(string securityKey)
    {
        var key = Encoding.UTF8.GetBytes(securityKey);
        var secret = new SymmetricSecurityKey(key);

        return secret;
    }

    private double GetUnixEpoch(DateTime dateTime)
    {
        return Math.Floor((dateTime - new DateTime(1970, 1, 1)).TotalMilliseconds);
    }
    
    private StreamKey GenerateKey(string value, double expires)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(jwtSettings.Value.SecurityKey));
        var data = $"{value}{expires}";
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        var streamKey = Convert.ToBase64String(hash);
        return new StreamKey
        {
            Key = streamKey,
            Expires = expires
        };
    }
}