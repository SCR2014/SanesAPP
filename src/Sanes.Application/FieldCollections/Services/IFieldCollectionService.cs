using Sanes.Application.FieldCollections.DTOs;
using Sanes.Application.Payments.Models;

namespace Sanes.Application.FieldCollections.Services;

public interface IFieldCollectionService
{
    Task<FieldCollectionDailyResponse> GetDailyAsync(
        Guid tenantId,
        Guid appUserId,
        DateOnly date,
        CancellationToken cancellationToken = default);

    Task<FieldCollectionPaymentResponse> CreatePaymentAsync(
        Guid tenantId,
        Guid appUserId,
        CreateFieldCollectionPaymentRequest request,
        PaymentIdempotencyContext idempotencyContext,
        CancellationToken cancellationToken = default);
}