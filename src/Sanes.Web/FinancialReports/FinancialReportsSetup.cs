using Sanes.Application.CollectionRoutes.DTOs;
using Sanes.Application.Investors.DTOs;
using Sanes.Application.Tenants.DTOs;
using Sanes.Application.AppUsers.DTOs;

namespace Sanes.Web.FinancialReports;

public sealed class FinancialReportsSetup
{
    public TenantDto Tenant { get; init; } =
        new();

    public List<InvestorResponse> Investors { get; init; } =
        new();

    public List<CollectionRouteResponse> CollectionRoutes { get; init; } =
        new();

    public List<AppUserResponse> Collectors { get; init; } =
        new();
}