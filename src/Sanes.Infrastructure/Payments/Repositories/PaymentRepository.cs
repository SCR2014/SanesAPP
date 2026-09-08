using Microsoft.EntityFrameworkCore;
using Sanes.Application.Payments.Repositories;
using Sanes.Domain.Entities;
using Sanes.Infrastructure.Persistence;

namespace Sanes.Infrastructure.Payments.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly SanesDbContext _dbContext;

    public PaymentRepository(SanesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(
        Payment payment,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Payments.AddAsync(
            payment,
            cancellationToken);
    }

    public async Task<List<Payment>> GetAllByLoanAsync(
        Guid tenantId,
        Guid loanId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Payments
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.LoanId == loanId)
            .OrderBy(x => x.PaymentDate)
            .ThenBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Payment?> GetByIdAsync(
        Guid tenantId,
        Guid paymentId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Payments
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x =>
                    x.TenantId == tenantId &&
                    x.Id == paymentId,
                cancellationToken);
    }

    public async Task<decimal> GetTotalPaidAsync(
        Guid tenantId,
        Guid loanId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Payments
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.LoanId == loanId)
            .SumAsync(
                x => (decimal?)x.Amount,
                cancellationToken)
            ?? 0m;
    }

    public async Task<Dictionary<Guid, decimal>> GetTotalPaidByLoansAsync(
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

        return await _dbContext.Payments
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                ids.Contains(x.LoanId))
            .GroupBy(x => x.LoanId)
            .Select(group => new
            {
                LoanId = group.Key,
                TotalPaid = group.Sum(x => x.Amount)
            })
            .ToDictionaryAsync(
                x => x.LoanId,
                x => x.TotalPaid,
                cancellationToken);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(
            cancellationToken);
    }
}