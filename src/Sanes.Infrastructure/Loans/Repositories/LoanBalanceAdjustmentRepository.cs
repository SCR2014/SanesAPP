using Microsoft.EntityFrameworkCore;
using Sanes.Application.Loans.Repositories;
using Sanes.Domain.Entities;
using Sanes.Domain.Enums;
using Sanes.Infrastructure.Persistence;

namespace Sanes.Infrastructure.Loans.Repositories;

public class LoanBalanceAdjustmentRepository
    : ILoanBalanceAdjustmentRepository
{
    private readonly SanesDbContext _dbContext;

    public LoanBalanceAdjustmentRepository(
        SanesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(
        LoanBalanceAdjustment adjustment,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.LoanBalanceAdjustments.AddAsync(
            adjustment,
            cancellationToken);
    }

    public async Task<decimal> GetTotalReductionsByLoanAsync(
        Guid tenantId,
        Guid loanId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.LoanBalanceAdjustments
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.LoanId == loanId &&
                x.AdjustmentType ==
                    LoanBalanceAdjustmentType.EarlySettlementDiscount)
            .SumAsync(
                x => (decimal?)x.Amount,
                cancellationToken)
            ?? 0m;
    }

    public async Task<Dictionary<Guid, decimal>>
        GetTotalReductionsByLoansAsync(
            Guid tenantId,
            IEnumerable<Guid> loanIds,
            CancellationToken cancellationToken = default)
    {
        var ids = loanIds
            .Distinct()
            .ToList();

        if (ids.Count == 0)
        {
            return new Dictionary<Guid, decimal>();
        }

        return await _dbContext.LoanBalanceAdjustments
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                ids.Contains(x.LoanId) &&
                x.AdjustmentType ==
                    LoanBalanceAdjustmentType.EarlySettlementDiscount)
            .GroupBy(x => x.LoanId)
            .Select(group => new
            {
                LoanId = group.Key,
                TotalReduction =
                    group.Sum(x => x.Amount)
            })
            .ToDictionaryAsync(
                x => x.LoanId,
                x => x.TotalReduction,
                cancellationToken);
    }

    public async Task<List<LoanBalanceAdjustment>>
        GetByLoanAsync(
            Guid tenantId,
            Guid loanId,
            CancellationToken cancellationToken = default)
    {
        return await _dbContext.LoanBalanceAdjustments
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.LoanId == loanId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(
            cancellationToken);
    }
}