using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Sanes.Application.Authentication.Services;
using Sanes.Domain.Entities;

namespace Sanes.Infrastructure.Authentication.Services;

public class JwtTokenService : IJwtTokenService
{
    private readonly JwtSettings _settings;

    public JwtTokenService(
        IOptions<JwtSettings> options)
    {
        _settings = options.Value;
    }

    public (string Token, DateTime ExpiresAt) GenerateToken(
        AppUser appUser)
    {
        if (string.IsNullOrWhiteSpace(_settings.Key))
        {
            throw new InvalidOperationException(
                "JWT signing key is not configured.");
        }

        var now = DateTime.UtcNow;

        var expiresAt =
            now.AddMinutes(_settings.ExpirationMinutes);

        var claims = new[]
        {
            new Claim(
                JwtRegisteredClaimNames.Sub,
                appUser.Id.ToString()),

            new Claim(
                "tenant_id",
                appUser.TenantId.ToString()),

            new Claim(
                ClaimTypes.NameIdentifier,
                appUser.Id.ToString()),

            new Claim(
                ClaimTypes.Name,
                appUser.Name),

            new Claim(
                "username",
                appUser.Username),

            new Claim(
                ClaimTypes.Role,
                appUser.Role.ToString())
        };

        var key =
            new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_settings.Key));

        var credentials =
            new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256);

        var token =
            new JwtSecurityToken(
                issuer: _settings.Issuer,
                audience: _settings.Audience,
                claims: claims,
                notBefore: now,
                expires: expiresAt,
                signingCredentials: credentials);

        return (
            new JwtSecurityTokenHandler()
                .WriteToken(token),
            expiresAt);
    }
}