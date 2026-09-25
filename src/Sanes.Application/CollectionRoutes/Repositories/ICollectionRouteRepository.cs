using Sanes.Domain.Entities;

namespace Sanes.Application.CollectionRoutes.Repositories;

public interface ICollectionRouteRepository
{
    Task<CollectionRoute?> GetByIdAsync(
        Guid id,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<List<CollectionRoute>> GetActiveByTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<List<CollectionRoute>> GetByTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default,
        bool includeInactive = false);

    Task<CollectionRoute?> GetByIdIncludingInactiveAsync(
        Guid id,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        CollectionRoute collectionRoute,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}