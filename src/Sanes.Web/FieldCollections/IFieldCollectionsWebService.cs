using Sanes.Application.FieldCollections.DTOs;
using Sanes.Application.Payments.DTOs;

namespace Sanes.Web.FieldCollections;

public interface IFieldCollectionsWebService
{
    Task<FieldCollectionDailyResponse> GetDailyAsync(
        DateOnly date,
        CancellationToken cancellationToken = default);

    Task<FieldCollectionPaymentResponse> CreatePaymentAsync(
        CreateFieldCollectionPaymentRequest request,
        CancellationToken cancellationToken = default);

    Task<PaymentReceiptResponse?> GetReceiptByPaymentAsync(
        Guid paymentId,
        CancellationToken cancellationToken = default);
}