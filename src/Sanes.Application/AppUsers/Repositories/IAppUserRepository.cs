using Sanes.Domain.Entities;
using Sanes.Domain.Enums;

namespace Sanes.Application.AppUsers.Repositories;

public interface IAppUserRepository
{
    Task<AppUser?> GetByIdAsync(
        Guid id,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<AppUser?> GetByIdIncludingInactiveAsync(
        Guid id,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<AppUser?> GetByUsernameAsync(
        Guid tenantId,
        string username,
        CancellationToken cancellationToken = default);

    Task<List<AppUser>> GetByTenantAsync(
        Guid tenantId,
        AppUserRole? role = null,
        bool includeInactive = false,
        CancellationToken cancellationToken = default);

    Task<int> CountActiveAdministratorsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<bool> UsernameExistsAsync(
        Guid tenantId,
        string username,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        AppUser appUser,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}