using Sanes.Application.Authentication.DTOs;

namespace Sanes.Application.Authentication.Services;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default);
}