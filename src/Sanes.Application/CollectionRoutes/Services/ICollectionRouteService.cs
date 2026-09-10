using Sanes.Application.CollectionRoutes.DTOs;

namespace Sanes.Application.CollectionRoutes.Services;

public interface ICollectionRouteService
{
    Task<CollectionRouteResponse> CreateAsync(
        CreateCollectionRouteRequest request,
        CancellationToken cancellationToken = default);

    Task<List<CollectionRouteResponse>> GetAllAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<CollectionRouteResponse?> GetByIdAsync(
        Guid id,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<CollectionRouteResponse?> UpdateAsync(
        Guid id,
        Guid tenantId,
        UpdateCollectionRouteRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid id,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<CollectionRouteResponse?> ReactivateAsync(
        Guid id,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<bool> ReorderClientsAsync(
        Guid id,
        Guid tenantId,
        ReorderCollectionRouteRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> OptimizeClientsAsync(
        Guid id,
        Guid tenantId,
        OptimizeCollectionRouteRequest request,
        CancellationToken cancellationToken = default);
}