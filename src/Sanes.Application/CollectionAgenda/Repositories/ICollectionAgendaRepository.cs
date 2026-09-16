using Sanes.Domain.Entities;
using Sanes.Domain.Enums;

namespace Sanes.Application.CollectionAgenda.Repositories;

public interface ICollectionAgendaRepository
{
    Task<List<CollectionRouteSchedule>> GetSchedulesForDayAsync(
        Guid tenantId,
        CollectionWeekDay dayOfWeek,
        CancellationToken cancellationToken = default);

    Task<List<AppUserCollectionRoute>> GetCollectorsForRoutesAsync(
        IReadOnlyCollection<Guid> collectionRouteIds,
        Guid tenantId,
        Guid? appUserId = null,
        CancellationToken cancellationToken = default);

    Task<List<Client>> GetClientsForRoutesAsync(
        IReadOnlyCollection<Guid> collectionRouteIds,
        Guid tenantId,
        CancellationToken cancellationToken = default);
}