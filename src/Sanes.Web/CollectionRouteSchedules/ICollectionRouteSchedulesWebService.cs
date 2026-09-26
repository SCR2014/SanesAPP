using Sanes.Application.CollectionRouteSchedules.DTOs;

namespace Sanes.Web.CollectionRouteSchedules;

public interface ICollectionRouteSchedulesWebService
{
    Task<List<CollectionRouteScheduleResponse>> GetAllAsync(
        Guid collectionRouteId,
        bool includeInactive = false,
        CancellationToken cancellationToken = default);

    Task<CollectionRouteScheduleResponse> CreateAsync(
        CreateCollectionRouteScheduleRequest request,
        CancellationToken cancellationToken = default);

    Task<CollectionRouteScheduleResponse?> UpdateAsync(
        Guid scheduleId,
        Guid collectionRouteId,
        UpdateCollectionRouteScheduleRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid scheduleId,
        Guid collectionRouteId,
        CancellationToken cancellationToken = default);

    Task<CollectionRouteScheduleResponse?> ReactivateAsync(
        Guid scheduleId,
        Guid collectionRouteId,
        CancellationToken cancellationToken = default);
}