using Sanes.Application.Dashboard.DTOs;

namespace Sanes.Application.Dashboard.Services;

public interface IFinancialDashboardService
{
    Task<FinancialDashboardSummaryResponse> GetSummaryAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<FinancialDashboardCashFlowResponse> GetCashFlowAsync(
        Guid tenantId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default);

    Task<List<FinancialDashboardInvestorBreakdownResponse>>
        GetInvestorsAsync(
            Guid tenantId,
            CancellationToken cancellationToken = default);

    Task<List<FinancialDashboardRouteBreakdownResponse>>
        GetRoutesAsync(
            Guid tenantId,
            CancellationToken cancellationToken = default);
}