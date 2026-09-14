using Sanes.Domain.Entities;

namespace Sanes.Application.Authentication.Services;

public interface IJwtTokenService
{
    (string Token, DateTime ExpiresAt) GenerateToken(
        AppUser appUser);
}