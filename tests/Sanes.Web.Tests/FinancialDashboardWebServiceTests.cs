using System.Net;
using System.Net.Http.Json;
using Sanes.Application.Dashboard.DTOs;
using Sanes.Application.Tenants.DTOs;
using Sanes.Web.Dashboard;

namespace Sanes.Web.Tests;

public class FinancialDashboardWebServiceTests
{
    [Fact]
    public async Task GetOverviewAsync_UsesCurrentTenantAndSummaryEndpoints()
    {
        var apiClient =
            new TestSanesApiClient();

        apiClient.EnqueueResponse(
            JsonResponse(
                new TenantDto
                {
                    Id = Guid.NewGuid(),
                    Name = "Sanes Test",
                    CurrencyCode = "DOP",
                    CurrencySymbol = "RD$",
                    IsActive = true
                }));

        apiClient.EnqueueResponse(
            JsonResponse(
                new FinancialDashboardSummaryResponse
                {
                    ActiveLoansCount = 4,
                    ActiveClientsCount = 3,
                    PrincipalOriginated = 10000m,
                    ContractualBalanceOutstanding = 7000m
                }));

        var service =
            new FinancialDashboardWebService(
                apiClient);

        var result =
            await service.GetOverviewAsync();

        Assert.Equal(
            "Sanes Test",
            result.Tenant.Name);

        Assert.Equal(
            4,
            result.Summary.ActiveLoansCount);

        Assert.Equal(
            10000m,
            result.Summary.PrincipalOriginated);

        Assert.Equal(
            2,
            apiClient.Requests.Count);

        AssertRequest(
            apiClient.Requests[0],
            "api/tenants/me");

        AssertRequest(
            apiClient.Requests[1],
            "api/dashboard/summary");
    }

    [Fact]
    public async Task GetCashFlowAsync_UsesRequestedPeriodAndDeserializesResponse()
    {
        var apiClient =
            new TestSanesApiClient();

        apiClient.EnqueueResponse(
            JsonResponse(
                new FinancialDashboardCashFlowResponse
                {
                    From =
                        new DateOnly(
                            2026,
                            9,
                            1),

                    To =
                        new DateOnly(
                            2026,
                            9,
                            21),

                    PrincipalOriginated =
                        5000m,

                    CashCollected =
                        2500m,

                    Items =
                    [
                        new FinancialDashboardCashFlowItemResponse
                        {
                            Date =
                                new DateOnly(
                                    2026,
                                    9,
                                    10),

                            PrincipalOriginated =
                                1000m,

                            CashCollected =
                                500m
                        }
                    ]
                }));

        var service =
            new FinancialDashboardWebService(
                apiClient);

        var result =
            await service.GetCashFlowAsync(
                new DateOnly(
                    2026,
                    9,
                    1),
                new DateOnly(
                    2026,
                    9,
                    21));

        Assert.Equal(
            5000m,
            result.PrincipalOriginated);

        Assert.Equal(
            2500m,
            result.CashCollected);

        Assert.Single(
            result.Items);

        Assert.Single(
            apiClient.Requests);

        var request =
            apiClient.Requests[0];

        Assert.Equal(
            HttpMethod.Get,
            request.Method);

        Assert.Equal(
            "api/dashboard/cash-flow" +
            "?from=2026-09-01" +
            "&to=2026-09-21",
            request.Uri);

        Assert.False(
            request.Uri.Contains(
                "tenantId",
                StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetCashFlowAsync_WhenApiReturnsBadRequest_ThrowsApiMessage()
    {
        var apiClient =
            new TestSanesApiClient();

        apiClient.EnqueueResponse(
            JsonResponse(
                new
                {
                    message =
                        "Cash flow period cannot exceed 366 days."
                },
                HttpStatusCode.BadRequest));

        var service =
            new FinancialDashboardWebService(
                apiClient);

        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () =>
                    service.GetCashFlowAsync(
                        new DateOnly(
                            2025,
                            9,
                            20),
                        new DateOnly(
                            2026,
                            9,
                            21)));

        Assert.Equal(
            "Cash flow period cannot exceed 366 days.",
            exception.Message);
    }

    [Fact]
    public async Task GetInvestorsAsync_UsesInvestorsEndpoint()
    {
        var apiClient =
            new TestSanesApiClient();

        apiClient.EnqueueResponse(
            JsonResponse(
                new List<
                    FinancialDashboardInvestorBreakdownResponse>
                {
                    new()
                    {
                        InvestorId =
                            Guid.NewGuid(),

                        InvestorName =
                            "Investor Test",

                        IsActive =
                            true,

                        ActiveLoansCount =
                            2,

                        TotalOutstanding =
                            3500m,

                        DelinquencyRate =
                            5.25m
                    }
                }));

        var service =
            new FinancialDashboardWebService(
                apiClient);

        var result =
            await service.GetInvestorsAsync();

        var investor =
            Assert.Single(
                result);

        Assert.Equal(
            "Investor Test",
            investor.InvestorName);

        Assert.Equal(
            3500m,
            investor.TotalOutstanding);

        Assert.Single(
            apiClient.Requests);

        AssertRequest(
            apiClient.Requests[0],
            "api/dashboard/investors");
    }

    [Fact]
    public async Task GetRoutesAsync_UsesRoutesEndpoint()
    {
        var apiClient =
            new TestSanesApiClient();

        apiClient.EnqueueResponse(
            JsonResponse(
                new List<
                    FinancialDashboardRouteBreakdownResponse>
                {
                    new()
                    {
                        CollectionRouteId =
                            null,

                        CollectionRouteName =
                            "Sin ruta",

                        IsActive =
                            false,

                        IsUnassigned =
                            true,

                        ActiveLoansCount =
                            1,

                        TotalOutstanding =
                            1200m
                    }
                }));

        var service =
            new FinancialDashboardWebService(
                apiClient);

        var result =
            await service.GetRoutesAsync();

        var route =
            Assert.Single(
                result);

        Assert.True(
            route.IsUnassigned);

        Assert.Null(
            route.CollectionRouteId);

        Assert.Equal(
            "Sin ruta",
            route.CollectionRouteName);

        Assert.Single(
            apiClient.Requests);

        AssertRequest(
            apiClient.Requests[0],
            "api/dashboard/routes");
    }

    private static void AssertRequest(
        TestSanesApiClient.RecordedRequest request,
        string expectedUri)
    {
        Assert.Equal(
            HttpMethod.Get,
            request.Method);

        Assert.Equal(
            expectedUri,
            request.Uri);

        Assert.False(
            request.Uri.Contains(
                "tenantId",
                StringComparison.OrdinalIgnoreCase));
    }

    private static HttpResponseMessage JsonResponse<T>(
        T value,
        HttpStatusCode statusCode =
            HttpStatusCode.OK)
    {
        return new HttpResponseMessage(
            statusCode)
        {
            Content =
                JsonContent.Create(
                    value)
        };
    }
}