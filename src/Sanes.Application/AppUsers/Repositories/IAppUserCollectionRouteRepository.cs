using Sanes.Domain.Entities;

namespace Sanes.Application.AppUsers.Repositories;

public interface IAppUserCollectionRouteRepository
{
    Task<List<AppUserCollectionRoute>> GetByAppUserAsync(
        Guid appUserId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        Guid appUserId,
        Guid collectionRouteId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        AppUserCollectionRoute assignment,
        CancellationToken cancellationToken = default);

    Task<AppUserCollectionRoute?> GetAsync(
        Guid appUserId,
        Guid collectionRouteId,
        CancellationToken cancellationToken = default);

    void Remove(AppUserCollectionRoute assignment);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}