using Sanes.Domain.Entities;

namespace Sanes.Application.Payments.Repositories;

public interface IPaymentReversalRepository
{
    Task AddAsync(
        PaymentReversal reversal,
        CancellationToken cancellationToken = default);

    Task<PaymentReversal?> GetByPaymentAsync(
        Guid tenantId,
        Guid paymentId,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}