using Sanes.Application.Payments.DTOs;

namespace Sanes.Application.Payments.Services;

public interface IPaymentReceiptService
{
    Task<PaymentReceiptResponse?>
        GetByPaymentAsync(
            Guid tenantId,
            Guid paymentId,
            CancellationToken cancellationToken = default);

    Task<PaymentReceiptResponse?>
        GetByIdAsync(
            Guid tenantId,
            Guid receiptId,
            CancellationToken cancellationToken = default);

    Task<PaymentReceiptResponse?>
        GetByNumberAsync(
            Guid tenantId,
            string receiptNumber,
            CancellationToken cancellationToken = default);
}