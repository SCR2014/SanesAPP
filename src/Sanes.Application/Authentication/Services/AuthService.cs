using Sanes.Application.AppUsers.Repositories;
using Sanes.Application.Authentication.DTOs;
using Sanes.Application.Tenants.Repositories;

namespace Sanes.Application.Authentication.Services;

public class AuthService : IAuthService
{
    private readonly IAppUserRepository _appUserRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly IPasswordService _passwordService;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthService(
        IAppUserRepository appUserRepository,
        ITenantRepository tenantRepository,
        IPasswordService passwordService,
        IJwtTokenService jwtTokenService)
    {
        _appUserRepository = appUserRepository;
        _tenantRepository = tenantRepository;
        _passwordService = passwordService;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<AuthResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.TenantId == Guid.Empty)
        {
            throw new ArgumentException(
                "Invalid credentials.");
        }

        if (string.IsNullOrWhiteSpace(request.Username) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ArgumentException(
                "Invalid credentials.");
        }

        var tenant =
            await _tenantRepository.GetByIdAsync(
                request.TenantId,
                cancellationToken);

        if (tenant is null)
        {
            throw new ArgumentException(
                "Invalid credentials.");
        }

        var normalizedUsername =
            request.Username
                .Trim()
                .ToLowerInvariant();

        var appUser =
            await _appUserRepository.GetByUsernameAsync(
                request.TenantId,
                normalizedUsername,
                cancellationToken);

        if (appUser is null ||
            !appUser.IsActive ||
            string.IsNullOrWhiteSpace(appUser.PasswordHash))
        {
            throw new ArgumentException(
                "Invalid credentials.");
        }

        var passwordValid =
            _passwordService.VerifyPassword(
                appUser.PasswordHash,
                request.Password);

        if (!passwordValid)
        {
            throw new ArgumentException(
                "Invalid credentials.");
        }

        var token =
            _jwtTokenService.GenerateToken(appUser);

        return new AuthResponse
        {
            AccessToken = token.Token,
            ExpiresAt = token.ExpiresAt,
            AppUserId = appUser.Id,
            TenantId = appUser.TenantId,
            Name = appUser.Name,
            Username = appUser.Username,
            Role = appUser.Role
        };
    }
}