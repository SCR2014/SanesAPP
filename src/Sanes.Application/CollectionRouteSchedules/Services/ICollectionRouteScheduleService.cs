using Sanes.Application.CollectionRouteSchedules.DTOs;

namespace Sanes.Application.CollectionRouteSchedules.Services;

public interface ICollectionRouteScheduleService
{
    Task<CollectionRouteScheduleResponse> CreateAsync(
        CreateCollectionRouteScheduleRequest request,
        CancellationToken cancellationToken = default);

    Task<List<CollectionRouteScheduleResponse>> GetAllAsync(
        Guid tenantId,
        Guid collectionRouteId,
        CancellationToken cancellationToken = default);

    Task<CollectionRouteScheduleResponse?> GetByIdAsync(
        Guid id,
        Guid tenantId,
        Guid collectionRouteId,
        CancellationToken cancellationToken = default);

    Task<CollectionRouteScheduleResponse?> UpdateAsync(
        Guid id,
        Guid tenantId,
        Guid collectionRouteId,
        UpdateCollectionRouteScheduleRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid id,
        Guid tenantId,
        Guid collectionRouteId,
        CancellationToken cancellationToken = default);

    Task<CollectionRouteScheduleResponse?> ReactivateAsync(
        Guid id,
        Guid tenantId,
        Guid collectionRouteId,
        CancellationToken cancellationToken = default);
}