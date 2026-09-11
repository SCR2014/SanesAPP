using Sanes.Domain.Enums;

namespace Sanes.Application.AppUsers.DTOs;

public class CreateAppUserRequest
{
    public Guid TenantId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public AppUserRole Role { get; set; }
}