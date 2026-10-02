using Sanes.Application.Payments.DTOs;

namespace Sanes.Application.Payments.Services;

public interface IPaymentReversalService
{
    Task<PaymentReversalResponse> ReverseAsync(
        Guid tenantId,
        Guid appUserId,
        Guid paymentId,
        PaymentReversalRequest request,
        CancellationToken cancellationToken = default);
}