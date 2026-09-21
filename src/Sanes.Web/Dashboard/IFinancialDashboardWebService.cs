using Sanes.Application.Dashboard.DTOs;

namespace Sanes.Web.Dashboard;

public interface IFinancialDashboardWebService
{
    Task<FinancialDashboardOverview> GetOverviewAsync(
        CancellationToken cancellationToken = default);

    Task<FinancialDashboardCashFlowResponse> GetCashFlowAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default);

    Task<List<FinancialDashboardInvestorBreakdownResponse>>
        GetInvestorsAsync(
            CancellationToken cancellationToken = default);

    Task<List<FinancialDashboardRouteBreakdownResponse>>
        GetRoutesAsync(
            CancellationToken cancellationToken = default);
}