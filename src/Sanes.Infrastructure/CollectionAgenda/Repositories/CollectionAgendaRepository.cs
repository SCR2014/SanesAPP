using Microsoft.EntityFrameworkCore;
using Sanes.Application.CollectionAgenda.Repositories;
using Sanes.Domain.Entities;
using Sanes.Domain.Enums;
using Sanes.Infrastructure.Persistence;

namespace Sanes.Infrastructure.CollectionAgenda.Repositories;

public class CollectionAgendaRepository
    : ICollectionAgendaRepository
{
    private readonly SanesDbContext _dbContext;

    public CollectionAgendaRepository(
        SanesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<CollectionRouteSchedule>> GetSchedulesForDayAsync(
        Guid tenantId,
        CollectionWeekDay dayOfWeek,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.CollectionRouteSchedules
            .AsNoTracking()
            .Include(x => x.CollectionRoute)
            .Where(x =>
                x.IsActive &&
                x.DayOfWeek == dayOfWeek &&
                x.CollectionRoute.IsActive &&
                x.CollectionRoute.TenantId == tenantId)
            .OrderBy(x => x.StartTime)
            .ThenBy(x => x.CollectionRoute.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<AppUserCollectionRoute>> GetCollectorsForRoutesAsync(
        IReadOnlyCollection<Guid> collectionRouteIds,
        Guid tenantId,
        Guid? appUserId = null,
        CancellationToken cancellationToken = default)
    {
        if (collectionRouteIds.Count == 0)
        {
            return new List<AppUserCollectionRoute>();
        }

        var query =
            _dbContext.AppUserCollectionRoutes
                .AsNoTracking()
                .Include(x => x.AppUser)
                .Include(x => x.CollectionRoute)
                .Where(x =>
                    collectionRouteIds.Contains(
                        x.CollectionRouteId) &&
                    x.CollectionRoute.IsActive &&
                    x.CollectionRoute.TenantId == tenantId &&
                    x.AppUser.IsActive &&
                    x.AppUser.TenantId == tenantId &&
                    x.AppUser.Role ==
                        AppUserRole.Collector);

        if (appUserId.HasValue)
        {
            query = query.Where(x =>
                x.AppUserId == appUserId.Value);
        }

        return await query
            .OrderBy(x => x.AppUser.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Client>> GetClientsForRoutesAsync(
        IReadOnlyCollection<Guid> collectionRouteIds,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        if (collectionRouteIds.Count == 0)
        {
            return new List<Client>();
        }

        return await _dbContext.Clients
            .AsNoTracking()
            .Where(x =>
                x.IsActive &&
                x.TenantId == tenantId &&
                x.CollectionRouteId.HasValue &&
                collectionRouteIds.Contains(
                    x.CollectionRouteId.Value))
            .OrderBy(x => x.CollectionRouteId)
            .ThenBy(x =>
                x.CollectionRouteOrder.HasValue
                    ? 0
                    : 1)
            .ThenBy(x => x.CollectionRouteOrder)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);
    }
}