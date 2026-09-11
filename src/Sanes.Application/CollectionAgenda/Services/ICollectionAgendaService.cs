using Sanes.Application.CollectionAgenda.DTOs;

namespace Sanes.Application.CollectionAgenda.Services;

public interface ICollectionAgendaService
{
    Task<CollectionAgendaResponse> GetDailyAgendaAsync(
        Guid tenantId,
        DateOnly date,
        Guid? appUserId = null,
        CancellationToken cancellationToken = default);
}