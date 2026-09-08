using Microsoft.EntityFrameworkCore;
using Sanes.Application.Investors.Repositories;
using Sanes.Domain.Entities;
using Sanes.Infrastructure.Persistence;

namespace Sanes.Infrastructure.Investors.Repositories;

public class InvestorRepository : IInvestorRepository
{
    private readonly SanesDbContext _dbContext;

    public InvestorRepository(SanesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(
        Investor investor,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Investors.AddAsync(
            investor,
            cancellationToken);
    }

    public async Task<List<Investor>> GetAllAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Investors
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Investor?> GetByIdAsync(
        Guid tenantId,
        Guid investorId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Investors
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x =>
                    x.TenantId == tenantId &&
                    x.Id == investorId &&
                    x.IsActive,
                cancellationToken);
    }

    public async Task<Investor?> GetByIdForUpdateAsync(
        Guid tenantId,
        Guid investorId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Investors
            .FirstOrDefaultAsync(
                x =>
                    x.TenantId == tenantId &&
                    x.Id == investorId,
                cancellationToken);
    }

    public async Task<bool> ExistsByIdentificationAsync(
    Guid tenantId,
    string identification,
    Guid? excludeInvestorId = null,
    CancellationToken cancellationToken = default)
{
    var query = _dbContext.Investors
        .AsNoTracking()
        .Where(x =>
            x.TenantId == tenantId &&
            x.Identification == identification);

    if (excludeInvestorId.HasValue)
    {
        query = query.Where(x =>
            x.Id != excludeInvestorId.Value);
    }

    return await query.AnyAsync(cancellationToken);
}

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(
            cancellationToken);
    }
}
