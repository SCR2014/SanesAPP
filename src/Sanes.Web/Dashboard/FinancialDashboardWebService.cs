using System.Net.Http.Json;
using Sanes.Application.Dashboard.DTOs;
using Sanes.Application.Tenants.DTOs;
using Sanes.Web.Api;
using System.Net;

namespace Sanes.Web.Dashboard;

public sealed class FinancialDashboardWebService
    : IFinancialDashboardWebService
{
    private readonly ISanesApiClient _apiClient;

    public FinancialDashboardWebService(
        ISanesApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<FinancialDashboardOverview> GetOverviewAsync(
        CancellationToken cancellationToken = default)
    {
        var tenant =
            await GetTenantAsync(
                cancellationToken);

        var summary =
            await GetSummaryAsync(
                cancellationToken);

        return new FinancialDashboardOverview
        {
            Tenant = tenant,
            Summary = summary
        };
    }

    public async Task<FinancialDashboardCashFlowResponse>
        GetCashFlowAsync(
            DateOnly from,
            DateOnly to,
            CancellationToken cancellationToken = default)
    {
        var url =
            $"api/dashboard/cash-flow" +
            $"?from={from:yyyy-MM-dd}" +
            $"&to={to:yyyy-MM-dd}";

        using var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                url);

        using var response =
            await _apiClient.SendAsync(
                request,
                cancellationToken);

        if (response.StatusCode ==
            HttpStatusCode.BadRequest)
        {
            var error =
                await response.Content
                    .ReadFromJsonAsync<ApiErrorResponse>(
                        cancellationToken);

            throw new ArgumentException(
                error?.Message
                ?? "El período seleccionado no es válido.");
        }

        response.EnsureSuccessStatusCode();

        var cashFlow =
            await response.Content
                .ReadFromJsonAsync<
                    FinancialDashboardCashFlowResponse>(
                        cancellationToken);

        return cashFlow
            ?? throw new InvalidOperationException(
                "Sanes.Api returned an empty cash flow response.");
    }

    public async Task<
    List<FinancialDashboardInvestorBreakdownResponse>>
        GetInvestorsAsync(
            CancellationToken cancellationToken = default)
    {
        using var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                "api/dashboard/investors");

        using var response =
            await _apiClient.SendAsync(
                request,
                cancellationToken);

        response.EnsureSuccessStatusCode();

        var investors =
            await response.Content
                .ReadFromJsonAsync<
                    List<FinancialDashboardInvestorBreakdownResponse>>(
                        cancellationToken);

        return investors
            ?? throw new InvalidOperationException(
                "Sanes.Api returned an empty investors dashboard response.");
    }

    public async Task<
        List<FinancialDashboardRouteBreakdownResponse>>
        GetRoutesAsync(
            CancellationToken cancellationToken = default)
    {
        using var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                "api/dashboard/routes");

        using var response =
            await _apiClient.SendAsync(
                request,
                cancellationToken);

        response.EnsureSuccessStatusCode();

        var routes =
            await response.Content
                .ReadFromJsonAsync<
                    List<FinancialDashboardRouteBreakdownResponse>>(
                        cancellationToken);

        return routes
            ?? throw new InvalidOperationException(
                "Sanes.Api returned an empty routes dashboard response.");
    }


    private async Task<TenantDto> GetTenantAsync(
        CancellationToken cancellationToken)
    {
        using var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                "api/tenants/me");

        using var response =
            await _apiClient.SendAsync(
                request,
                cancellationToken);

        response.EnsureSuccessStatusCode();

        var tenant =
            await response.Content
                .ReadFromJsonAsync<TenantDto>(
                    cancellationToken);

        return tenant
            ?? throw new InvalidOperationException(
                "Sanes.Api returned an empty tenant response.");
    }

    private async Task<FinancialDashboardSummaryResponse>
        GetSummaryAsync(
            CancellationToken cancellationToken)
    {
        using var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                "api/dashboard/summary");

        using var response =
            await _apiClient.SendAsync(
                request,
                cancellationToken);

        response.EnsureSuccessStatusCode();

        var summary =
            await response.Content
                .ReadFromJsonAsync<
                    FinancialDashboardSummaryResponse>(
                        cancellationToken);

        return summary
            ?? throw new InvalidOperationException(
                "Sanes.Api returned an empty dashboard summary.");
    }

    private sealed class ApiErrorResponse
    {
        public string? Message { get; set; }
    }
}