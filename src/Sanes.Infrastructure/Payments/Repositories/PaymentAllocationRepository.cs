using Microsoft.EntityFrameworkCore;
using Sanes.Application.Payments.Repositories;
using Sanes.Domain.Entities;
using Sanes.Domain.Enums;
using Sanes.Infrastructure.Persistence;

namespace Sanes.Infrastructure.Payments.Repositories;

public class PaymentAllocationRepository
    : IPaymentAllocationRepository
{
    private readonly SanesDbContext _dbContext;

    public PaymentAllocationRepository(
        SanesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(
        PaymentAllocation allocation,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.PaymentAllocations.AddAsync(
            allocation,
            cancellationToken);
    }

    public async Task AddRangeAsync(
        IEnumerable<PaymentAllocation> allocations,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.PaymentAllocations.AddRangeAsync(
            allocations,
            cancellationToken);
    }

    public async Task<List<PaymentAllocation>> GetByPaymentAsync(
        Guid tenantId,
        Guid paymentId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.PaymentAllocations
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.PaymentId == paymentId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<decimal> GetTotalAppliedToLoanAsync(
        Guid tenantId,
        Guid loanId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.PaymentAllocations
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.AllocationType ==
                    PaymentAllocationType.LoanBalance &&
                x.Payment.LoanId == loanId)
            .SumAsync(
                x => (decimal?)x.Amount,
                cancellationToken)
            ?? 0m;
    }

    public async Task<Dictionary<Guid, decimal>> GetTotalAppliedToLoansAsync(
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

        return await _dbContext.PaymentAllocations
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.AllocationType ==
                    PaymentAllocationType.LoanBalance &&
                ids.Contains(x.Payment.LoanId))
            .GroupBy(x => x.Payment.LoanId)
            .Select(group => new
            {
                LoanId = group.Key,
                TotalApplied = group.Sum(x => x.Amount)
            })
            .ToDictionaryAsync(
                x => x.LoanId,
                x => x.TotalApplied,
                cancellationToken);
    }

    public async Task<decimal> GetTotalAppliedToLateFeeAsync(
        Guid tenantId,
        Guid lateFeeChargeId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.PaymentAllocations
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.AllocationType ==
                    PaymentAllocationType.LateFee &&
                x.LateFeeChargeId == lateFeeChargeId)
            .SumAsync(
                x => (decimal?)x.Amount,
                cancellationToken)
            ?? 0m;
    }

    public async Task<Dictionary<Guid, decimal>> GetTotalAppliedToLateFeesAsync(
        Guid tenantId,
        IEnumerable<Guid> lateFeeChargeIds,
        CancellationToken cancellationToken = default)
    {
        var ids = lateFeeChargeIds
            .Distinct()
            .ToList();

        if (ids.Count == 0)
        {
            return new Dictionary<Guid, decimal>();
        }

        return await _dbContext.PaymentAllocations
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.AllocationType ==
                    PaymentAllocationType.LateFee &&
                x.LateFeeChargeId.HasValue &&
                ids.Contains(x.LateFeeChargeId.Value))
            .GroupBy(x => x.LateFeeChargeId!.Value)
            .Select(group => new
            {
                LateFeeChargeId = group.Key,
                TotalApplied = group.Sum(x => x.Amount)
            })
            .ToDictionaryAsync(
                x => x.LateFeeChargeId,
                x => x.TotalApplied,
                cancellationToken);
    }

    public async Task<List<PaymentAllocation>>
        GetLoanBalanceAllocationsByLoansAsync(
            Guid tenantId,
            IEnumerable<Guid> loanIds,
            CancellationToken cancellationToken = default)
    {
        var ids = loanIds
            .Distinct()
            .ToList();

        if (ids.Count == 0)
        {
            return new List<PaymentAllocation>();
        }

        return await _dbContext.PaymentAllocations
            .AsNoTracking()
            .Include(x => x.Payment)
            .Where(x =>
                x.TenantId == tenantId &&
                x.AllocationType ==
                    PaymentAllocationType.LoanBalance &&
                ids.Contains(x.Payment.LoanId))
            .OrderBy(x => x.Payment.PaymentDate)
            .ThenBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(
            cancellationToken);
    }
}