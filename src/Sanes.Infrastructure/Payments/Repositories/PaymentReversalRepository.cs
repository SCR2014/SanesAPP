using Microsoft.EntityFrameworkCore;
using Sanes.Application.Payments.Repositories;
using Sanes.Domain.Entities;
using Sanes.Infrastructure.Persistence;

namespace Sanes.Infrastructure.Payments.Repositories;

public class PaymentReversalRepository
    : IPaymentReversalRepository
{
    private readonly SanesDbContext _dbContext;

    public PaymentReversalRepository(
        SanesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(
        PaymentReversal reversal,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.PaymentReversals.AddAsync(
            reversal,
            cancellationToken);
    }

    public async Task<PaymentReversal?> GetByPaymentAsync(
        Guid tenantId,
        Guid paymentId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.PaymentReversals
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x =>
                    x.TenantId == tenantId &&
                    x.PaymentId == paymentId,
                cancellationToken);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(
            cancellationToken);
    }
}