using Sanes.Application.FieldCollections.DTOs;

namespace Sanes.Application.FieldCollections.Services;

public interface IFieldCollectionService
{
    Task<FieldCollectionDailyResponse> GetDailyAsync(
        Guid tenantId,
        Guid appUserId,
        DateOnly date,
        CancellationToken cancellationToken = default);

    Task<FieldCollectionPaymentResponse> CreatePaymentAsync(
        CreateFieldCollectionPaymentRequest request,
        CancellationToken cancellationToken = default);
}