using Sanes.Domain.Entities;
using Sanes.Domain.Enums;

namespace Sanes.Application.CollectionRouteSchedules.Repositories;

public interface ICollectionRouteScheduleRepository
{
    Task<CollectionRouteSchedule?> GetByIdAsync(
        Guid id,
        Guid collectionRouteId,
        CancellationToken cancellationToken = default);

    Task<CollectionRouteSchedule?> GetByIdIncludingInactiveAsync(
        Guid id,
        Guid collectionRouteId,
        CancellationToken cancellationToken = default);

    Task<List<CollectionRouteSchedule>> GetActiveByRouteAsync(
        Guid collectionRouteId,
        CancellationToken cancellationToken = default);

    Task<bool> DayExistsAsync(
        Guid collectionRouteId,
        CollectionWeekDay dayOfWeek,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        CollectionRouteSchedule schedule,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}