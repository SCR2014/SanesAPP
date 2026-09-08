using Microsoft.EntityFrameworkCore;
using Sanes.Application.Tenants.Repositories;
using Sanes.Domain.Entities;
using Sanes.Infrastructure.Persistence;

namespace Sanes.Infrastructure.Tenants.Repositories;

public class TenantRepository : ITenantRepository
{
    private readonly SanesDbContext _dbContext;

    public TenantRepository(SanesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(
        Tenant tenant,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Tenants.AddAsync(
            tenant,
            cancellationToken);
    }

    public async Task<List<Tenant>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Tenants
            .AsNoTracking()
            .Where(x => x.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Tenant?> GetByIdAsync(
    Guid id,
    CancellationToken cancellationToken = default)
{
    return await _dbContext.Tenants
        .AsNoTracking()
        .FirstOrDefaultAsync(
            x => x.Id == id && x.IsActive,
            cancellationToken);
}

    public async Task<Tenant?> GetByIdForUpdateAsync(
    Guid id,
    CancellationToken cancellationToken = default)
{
    return await _dbContext.Tenants
        .FirstOrDefaultAsync(
            x => x.Id == id,
            cancellationToken);
}
}