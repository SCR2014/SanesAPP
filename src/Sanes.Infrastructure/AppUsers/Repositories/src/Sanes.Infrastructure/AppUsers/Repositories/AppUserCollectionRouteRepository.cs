using Microsoft.EntityFrameworkCore;
using Sanes.Application.AppUsers.Repositories;
using Sanes.Domain.Entities;
using Sanes.Infrastructure.Persistence;

namespace Sanes.Infrastructure.AppUsers.Repositories;

public class AppUserCollectionRouteRepository
    : IAppUserCollectionRouteRepository
{
    private readonly SanesDbContext _dbContext;

    public AppUserCollectionRouteRepository(
        SanesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<AppUserCollectionRoute>> GetByAppUserAsync(
        Guid appUserId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.AppUserCollectionRoutes
            .AsNoTracking()
            .Include(x => x.CollectionRoute)
            .Where(x =>
                x.AppUserId == appUserId &&
                x.CollectionRoute.IsActive)
            .OrderBy(x => x.CollectionRoute.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(
        Guid appUserId,
        Guid collectionRouteId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.AppUserCollectionRoutes
            .AsNoTracking()
            .AnyAsync(
                x =>
                    x.AppUserId == appUserId &&
                    x.CollectionRouteId == collectionRouteId,
                cancellationToken);
    }

    public async Task AddAsync(
        AppUserCollectionRoute assignment,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.AppUserCollectionRoutes
            .AddAsync(
                assignment,
                cancellationToken);
    }

    public async Task<AppUserCollectionRoute?> GetAsync(
        Guid appUserId,
        Guid collectionRouteId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.AppUserCollectionRoutes
            .FirstOrDefaultAsync(
                x =>
                    x.AppUserId == appUserId &&
                    x.CollectionRouteId == collectionRouteId,
                cancellationToken);
    }

    public void Remove(
        AppUserCollectionRoute assignment)
    {
        _dbContext.AppUserCollectionRoutes
            .Remove(assignment);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(
            cancellationToken);
    }
}