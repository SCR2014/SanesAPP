using Microsoft.EntityFrameworkCore;
using Sanes.Application.Loans.Repositories;
using Sanes.Domain.Entities;
using Sanes.Infrastructure.Persistence;

namespace Sanes.Infrastructure.Loans.Repositories;

public class EarlySettlementRepository
    : IEarlySettlementRepository
{
    private readonly SanesDbContext _dbContext;

    public EarlySettlementRepository(
        SanesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<EarlySettlement?> GetByIdAsync(
        Guid tenantId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.EarlySettlements
            .AsNoTracking()
            .Include(x => x.Payment)
            .Include(x => x.LoanBalanceAdjustment)
            .FirstOrDefaultAsync(
                x =>
                    x.TenantId == tenantId &&
                    x.Id == id,
                cancellationToken);
    }

    public async Task<EarlySettlement?> GetByLoanAsync(
        Guid tenantId,
        Guid loanId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.EarlySettlements
            .AsNoTracking()
            .Include(x => x.Payment)
            .Include(x => x.LoanBalanceAdjustment)
            .FirstOrDefaultAsync(
                x =>
                    x.TenantId == tenantId &&
                    x.LoanId == loanId,
                cancellationToken);
    }

    public async Task<bool> ExistsForLoanAsync(
        Guid tenantId,
        Guid loanId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.EarlySettlements
            .AsNoTracking()
            .AnyAsync(
                x =>
                    x.TenantId == tenantId &&
                    x.LoanId == loanId,
                cancellationToken);
    }

    public async Task AddAsync(
        EarlySettlement earlySettlement,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.EarlySettlements.AddAsync(
            earlySettlement,
            cancellationToken);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(
            cancellationToken);
    }
}