using Sanes.Application.Clients.DTOs;

namespace Sanes.Application.Clients.Services;

public interface IClientService
{
    Task<ClientResponse> CreateAsync(
        CreateClientRequest request,
        CancellationToken cancellationToken = default);

    Task<List<ClientResponse>> GetAllAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<ClientResponse?> GetByIdAsync(
        Guid tenantId,
        Guid clientId,
        CancellationToken cancellationToken = default);

    Task<ClientResponse?> UpdateAsync(
        Guid tenantId,
        Guid clientId,
        UpdateClientRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid tenantId,
        Guid clientId,
        CancellationToken cancellationToken = default);

    Task<bool> ReactivateAsync(
        Guid tenantId,
        Guid clientId,
        CancellationToken cancellationToken = default);
}