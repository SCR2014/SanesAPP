using Sanes.Domain.Enums;

namespace Sanes.Application.Authentication.DTOs;

public class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public Guid AppUserId { get; set; }

    public Guid TenantId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public AppUserRole Role { get; set; }
}