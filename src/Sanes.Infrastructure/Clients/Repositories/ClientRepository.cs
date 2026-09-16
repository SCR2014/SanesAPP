using Microsoft.EntityFrameworkCore;
using Sanes.Application.Clients.Repositories;
using Sanes.Domain.Entities;
using Sanes.Infrastructure.Persistence;

namespace Sanes.Infrastructure.Clients.Repositories;

public class ClientRepository : IClientRepository
{
    private readonly SanesDbContext _dbContext;

    public ClientRepository(SanesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(
        Client client,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Clients.AddAsync(client, cancellationToken);
    }

    public async Task<List<Client>> GetAllAsync(
        Guid tenantId,
        Guid? collectionRouteId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Clients
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.IsActive);

        if (collectionRouteId.HasValue)
            query = query.Where(
                x => x.CollectionRouteId == collectionRouteId.Value);

        if (collectionRouteId.HasValue)
        {
            return await query
                .OrderBy(x => x.CollectionRouteOrder == null)
                .ThenBy(x => x.CollectionRouteOrder)
                .ThenBy(x => x.FirstName)
                .ThenBy(x => x.LastName)
                .ToListAsync(cancellationToken);
        }

        return await query
            .OrderBy(x => x.FirstName)
            .ThenBy(x => x.LastName)
            .ToListAsync(cancellationToken);
    }

    public async Task<Client?> GetByIdAsync(
        Guid tenantId,
        Guid clientId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Clients
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x =>
                    x.TenantId == tenantId &&
                    x.Id == clientId &&
                    x.IsActive,
                cancellationToken);
    }

    public async Task<Client?> GetByIdForUpdateAsync(
        Guid tenantId,
        Guid clientId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Clients
            .FirstOrDefaultAsync(
                x =>
                    x.TenantId == tenantId &&
                    x.Id == clientId,
                cancellationToken);
    }

    public async Task<bool> ExistsByIdentificationAsync(
        Guid tenantId,
        string identification,
        Guid? excludeClientId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Clients
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.Identification == identification);

        if (excludeClientId.HasValue)
        {
            query = query.Where(x =>
                x.Id != excludeClientId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<List<Client>> GetByCollectionRouteForUpdateAsync(
        Guid tenantId,
        Guid collectionRouteId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Clients
            .Where(x =>
                x.TenantId == tenantId &&
                x.CollectionRouteId == collectionRouteId &&
                x.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}