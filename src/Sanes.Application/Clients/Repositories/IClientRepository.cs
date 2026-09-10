using Sanes.Domain.Entities;

namespace Sanes.Application.Clients.Repositories;

public interface IClientRepository
{
    Task AddAsync(
        Client client,
        CancellationToken cancellationToken = default);

    Task<List<Client>> GetAllAsync(
        Guid tenantId,
        Guid? collectionRouteId = null,
        CancellationToken cancellationToken = default);

    Task<Client?> GetByIdAsync(
        Guid tenantId,
        Guid clientId,
        CancellationToken cancellationToken = default);

    Task<Client?> GetByIdForUpdateAsync(
        Guid tenantId,
        Guid clientId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByIdentificationAsync(
        Guid tenantId,
        string identification,
        Guid? excludeClientId = null,
        CancellationToken cancellationToken = default);

    Task<List<Client>> GetByCollectionRouteForUpdateAsync(
        Guid tenantId,
        Guid collectionRouteId,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}