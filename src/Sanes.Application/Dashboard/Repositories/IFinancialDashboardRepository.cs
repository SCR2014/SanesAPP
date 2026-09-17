using Sanes.Application.Dashboard.Models;

namespace Sanes.Application.Dashboard.Repositories;

public interface IFinancialDashboardRepository
{
    Task<FinancialDashboardHistoricalData>
        GetHistoricalDataAsync(
            Guid tenantId,
            CancellationToken cancellationToken = default);

    Task<List<FinancialDashboardCashFlowDataItem>>
        GetCashFlowAsync(
            Guid tenantId,
            DateOnly from,
            DateOnly to,
            CancellationToken cancellationToken = default);
}