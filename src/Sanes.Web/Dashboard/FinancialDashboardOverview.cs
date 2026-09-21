using Sanes.Application.Dashboard.DTOs;
using Sanes.Application.Tenants.DTOs;

namespace Sanes.Web.Dashboard;

public sealed class FinancialDashboardOverview
{
    public TenantDto Tenant { get; init; } = new();

    public FinancialDashboardSummaryResponse Summary { get; init; } =
        new();
}