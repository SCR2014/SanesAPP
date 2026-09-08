using Sanes.Domain.Entities;

namespace Sanes.Application.Tenants.Repositories;

public interface ITenantRepository
{
    Task AddAsync(
        Tenant tenant,
        CancellationToken cancellationToken = default);

    Task<List<Tenant>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<Tenant?> GetByIdAsync(
    Guid id,
    CancellationToken cancellationToken = default);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);

    Task<Tenant?> GetByIdForUpdateAsync(
    Guid id,
    CancellationToken cancellationToken = default);
        
}