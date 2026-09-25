using Sanes.Application.Clients.DTOs;
using Sanes.Application.CollectionRoutes.DTOs;

namespace Sanes.Web.Clients;

public interface IClientsWebService
{
    Task<List<ClientResponse>> GetAllAsync(
        Guid? collectionRouteId = null,
        bool includeInactive = false,
        CancellationToken cancellationToken = default);

    Task<List<CollectionRouteResponse>> GetRoutesAsync(
        CancellationToken cancellationToken = default);

    Task<ClientResponse> CreateAsync(
        CreateClientRequest request,
        CancellationToken cancellationToken = default);

    Task<ClientResponse?> UpdateAsync(
        Guid clientId,
        UpdateClientRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid clientId,
        CancellationToken cancellationToken = default);

    Task<bool> ReactivateAsync(
        Guid clientId,
        CancellationToken cancellationToken = default);
}