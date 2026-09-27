using Sanes.Application.Payments.DTOs;

namespace Sanes.Web.PaymentReceipts;

public interface IPaymentReceiptsWebService
{
    Task<PaymentReceiptListResponse> GetPagedAsync(
        PaymentReceiptListRequest request,
        CancellationToken cancellationToken = default);

    Task<PaymentReceiptResponse?> GetByIdAsync(
        Guid receiptId,
        CancellationToken cancellationToken = default);

    Task<PaymentReceiptResponse?> GetByNumberAsync(
        string receiptNumber,
        CancellationToken cancellationToken = default);
}