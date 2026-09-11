using Microsoft.EntityFrameworkCore;
using Sanes.Application.CollectionRouteSchedules.Repositories;
using Sanes.Domain.Entities;
using Sanes.Domain.Enums;
using Sanes.Infrastructure.Persistence;

namespace Sanes.Infrastructure.CollectionRouteSchedules.Repositories;

public class CollectionRouteScheduleRepository
    : ICollectionRouteScheduleRepository
{
    private readonly SanesDbContext _dbContext;

    public CollectionRouteScheduleRepository(
        SanesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CollectionRouteSchedule?> GetByIdAsync(
        Guid id,
        Guid collectionRouteId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.CollectionRouteSchedules
            .FirstOrDefaultAsync(
                x =>
                    x.Id == id &&
                    x.CollectionRouteId == collectionRouteId &&
                    x.IsActive,
                cancellationToken);
    }

    public async Task<CollectionRouteSchedule?> GetByIdIncludingInactiveAsync(
        Guid id,
        Guid collectionRouteId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.CollectionRouteSchedules
            .FirstOrDefaultAsync(
                x =>
                    x.Id == id &&
                    x.CollectionRouteId == collectionRouteId,
                cancellationToken);
    }

    public async Task<List<CollectionRouteSchedule>> GetActiveByRouteAsync(
        Guid collectionRouteId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.CollectionRouteSchedules
            .AsNoTracking()
            .Where(x =>
                x.CollectionRouteId == collectionRouteId &&
                x.IsActive)
            .OrderBy(x => x.DayOfWeek)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> DayExistsAsync(
        Guid collectionRouteId,
        CollectionWeekDay dayOfWeek,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        var query =
            _dbContext.CollectionRouteSchedules
                .AsNoTracking()
                .Where(x =>
                    x.CollectionRouteId == collectionRouteId &&
                    x.DayOfWeek == dayOfWeek);

        if (excludeId.HasValue)
        {
            query = query.Where(x =>
                x.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task AddAsync(
        CollectionRouteSchedule schedule,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.CollectionRouteSchedules
            .AddAsync(
                schedule,
                cancellationToken);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(
            cancellationToken);
    }
}