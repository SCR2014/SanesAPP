using Microsoft.EntityFrameworkCore;
using Sanes.Application.LateFees.Repositories;
using Sanes.Domain.Entities;
using Sanes.Infrastructure.Persistence;

namespace Sanes.Infrastructure.LateFees.Repositories;

public class LateFeeRepository : ILateFeeRepository
{
    private readonly SanesDbContext _dbContext;

    public LateFeeRepository(SanesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddChargeAsync(
        LateFeeCharge charge,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.LateFeeCharges.AddAsync(
            charge,
            cancellationToken);
    }

    public async Task AddAdjustmentAsync(
        LateFeeAdjustment adjustment,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.LateFeeAdjustments.AddAsync(
            adjustment,
            cancellationToken);
    }

    public async Task<List<LateFeeCharge>> GetByLoanAsync(
        Guid tenantId,
        Guid loanId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.LateFeeCharges
            .AsNoTracking()
            .Include(x => x.Adjustments)
            .Where(x =>
                x.TenantId == tenantId &&
                x.LoanId == loanId)
            .OrderBy(x => x.EffectiveDate)
            .ThenBy(x => x.InstallmentNumber)
            .ThenBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<LateFeeCharge>> GetByLoansAsync(
        Guid tenantId,
        IEnumerable<Guid> loanIds,
        CancellationToken cancellationToken = default)
    {
        var ids = loanIds
            .Distinct()
            .ToList();

        if (ids.Count == 0)
        {
            return new List<LateFeeCharge>();
        }

        return await _dbContext.LateFeeCharges
            .AsNoTracking()
            .Include(x => x.Adjustments)
            .Where(x =>
                x.TenantId == tenantId &&
                ids.Contains(x.LoanId))
            .OrderBy(x => x.EffectiveDate)
            .ThenBy(x => x.InstallmentNumber)
            .ThenBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<LateFeeCharge?> GetChargeByIdAsync(
        Guid tenantId,
        Guid chargeId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.LateFeeCharges
            .AsNoTracking()
            .Include(x => x.Adjustments)
            .FirstOrDefaultAsync(
                x =>
                    x.TenantId == tenantId &&
                    x.Id == chargeId,
                cancellationToken);
    }

    public async Task<LateFeeCharge?> GetChargeForUpdateAsync(
        Guid tenantId,
        Guid chargeId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.LateFeeCharges
            .Include(x => x.Adjustments)
            .FirstOrDefaultAsync(
                x =>
                    x.TenantId == tenantId &&
                    x.Id == chargeId,
                cancellationToken);
    }

    public async Task<bool> ExistsAsync(
        Guid tenantId,
        Guid loanId,
        int installmentNumber,
        DateTime effectiveDate,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.LateFeeCharges
            .AsNoTracking()
            .AnyAsync(
                x =>
                    x.TenantId == tenantId &&
                    x.LoanId == loanId &&
                    x.InstallmentNumber == installmentNumber &&
                    x.EffectiveDate == effectiveDate,
                cancellationToken);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(
            cancellationToken);
    }
}