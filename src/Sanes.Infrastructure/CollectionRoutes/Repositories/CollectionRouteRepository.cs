using Microsoft.EntityFrameworkCore;
using Sanes.Application.CollectionRoutes.Repositories;
using Sanes.Domain.Entities;
using Sanes.Infrastructure.Persistence;

namespace Sanes.Infrastructure.CollectionRoutes.Repositories;

public class CollectionRouteRepository : ICollectionRouteRepository
{
    private readonly SanesDbContext _dbContext;

    public CollectionRouteRepository(SanesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CollectionRoute?> GetByIdAsync(
        Guid id,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.CollectionRoutes
            .FirstOrDefaultAsync(
                x => x.Id == id &&
                     x.TenantId == tenantId &&
                     x.IsActive,
                cancellationToken);
    }

    public async Task<List<CollectionRoute>> GetActiveByTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.CollectionRoutes
            .Where(x => x.TenantId == tenantId && x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<CollectionRoute?> GetByIdIncludingInactiveAsync(
        Guid id,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.CollectionRoutes
            .FirstOrDefaultAsync(
                x => x.Id == id &&
                     x.TenantId == tenantId,
                cancellationToken);
    }

    public async Task AddAsync(
        CollectionRoute collectionRoute,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.CollectionRoutes.AddAsync(
            collectionRoute,
            cancellationToken);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}