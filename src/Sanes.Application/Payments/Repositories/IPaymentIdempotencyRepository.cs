using Sanes.Application.Payments.Models;

namespace Sanes.Application.Payments.Repositories;

public interface IPaymentIdempotencyRepository
{
    Task<PaymentIdempotencyClaimResult> ClaimAsync(
        Guid tenantId,
        Guid idempotencyKey,
        string requestHash,
        CancellationToken cancellationToken = default);
}