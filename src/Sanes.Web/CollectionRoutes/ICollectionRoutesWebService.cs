using Sanes.Application.CollectionRoutes.DTOs;

namespace Sanes.Web.CollectionRoutes;

public interface ICollectionRoutesWebService
{
    Task<List<CollectionRouteResponse>> GetAllAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default);

    Task<CollectionRouteResponse> CreateAsync(
        CreateCollectionRouteRequest request,
        CancellationToken cancellationToken = default);

    Task<CollectionRouteResponse?> UpdateAsync(
        Guid routeId,
        UpdateCollectionRouteRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid routeId,
        CancellationToken cancellationToken = default);

    Task<CollectionRouteResponse?> ReactivateAsync(
        Guid routeId,
        CancellationToken cancellationToken = default);

    Task<bool> ReorderClientsAsync(
        Guid routeId,
        IReadOnlyCollection<Guid> clientIds,
        CancellationToken cancellationToken = default);

    Task<bool> OptimizeAsync(
        Guid routeId,
        OptimizeCollectionRouteRequest request,
        CancellationToken cancellationToken = default);
}