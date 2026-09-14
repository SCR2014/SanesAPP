using Sanes.Domain.Enums;

namespace Sanes.Application.Authentication.Services;

public interface ICurrentUserService
{
    bool IsAuthenticated { get; }

    Guid AppUserId { get; }

    Guid TenantId { get; }

    string? Username { get; }

    AppUserRole Role { get; }
}