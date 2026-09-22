using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Sanes.Application.AppUsers.DTOs;
using Sanes.Application.CollectionRoutes.DTOs;
using Sanes.Application.FinancialReports.DTOs;
using Sanes.Application.Investors.DTOs;
using Sanes.Application.Tenants.DTOs;
using Sanes.Web.FinancialReports;

namespace Sanes.Web.Tests;

public class FinancialReportsWebServiceTests
{
    [Fact]
    public async Task GetSetupAsync_LoadsTenantInvestorsRoutesAndCollectors()
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
                new List<InvestorResponse>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        Name = "Investor Test",
                        IsActive = true
                    }
                }));

        apiClient.EnqueueResponse(
            JsonResponse(
                new List<CollectionRouteResponse>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        Name = "Ruta Test",
                        IsActive = true
                    }
                }));

        apiClient.EnqueueResponse(
            JsonResponse(
                new List<AppUserResponse>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        Name = "Cobrador Test",
                        Username = "collector"
                    }
                }));

        var service =
            new FinancialReportsWebService(
                apiClient);

        var result =
            await service.GetSetupAsync();

        Assert.Equal(
            "Sanes Test",
            result.Tenant.Name);

        Assert.Single(
            result.Investors);

        Assert.Single(
            result.CollectionRoutes);

        Assert.Single(
            result.Collectors);

        Assert.Equal(
            4,
            apiClient.Requests.Count);

        AssertRequest(
            apiClient.Requests[0],
            "api/tenants/me");

        AssertRequest(
            apiClient.Requests[1],
            "api/investors");

        AssertRequest(
            apiClient.Requests[2],
            "api/collection-routes");

        AssertRequest(
            apiClient.Requests[3],
            "api/app-users?role=Collector");
    }

    [Fact]
    public async Task GetPortfolioAsync_WithFilters_BuildsExpectedUrl()
    {
        var apiClient =
            new TestSanesApiClient();

        var investorId =
            Guid.NewGuid();

        var routeId =
            Guid.NewGuid();

        apiClient.EnqueueResponse(
            JsonResponse(
                new FinancialPortfolioReportResponse
                {
                    Summary =
                        new FinancialPortfolioReportSummaryResponse
                        {
                            LoansCount = 2,
                            TotalOutstanding = 5000m
                        }
                }));

        var service =
            new FinancialReportsWebService(
                apiClient);

        var result =
            await service.GetPortfolioAsync(
                investorId,
                routeId,
                true);

        Assert.Equal(
            2,
            result.Summary.LoansCount);

        Assert.Single(
            apiClient.Requests);

        AssertRequest(
            apiClient.Requests[0],
            "api/reports/portfolio" +
            $"?investorId={investorId:D}" +
            $"&collectionRouteId={routeId:D}" +
            "&overdueOnly=true");
    }

    [Fact]
    public async Task GetPortfolioAsync_WithoutFilters_DoesNotSendTenantId()
    {
        var apiClient =
            new TestSanesApiClient();

        apiClient.EnqueueResponse(
            JsonResponse(
                new FinancialPortfolioReportResponse()));

        var service =
            new FinancialReportsWebService(
                apiClient);

        await service.GetPortfolioAsync(
            null,
            null,
            false);

        var request =
            Assert.Single(
                apiClient.Requests);

        AssertRequest(
            request,
            "api/reports/portfolio");
    }

    [Fact]
    public async Task GetDelinquencyAsync_WithFilters_BuildsExpectedUrl()
    {
        var apiClient =
            new TestSanesApiClient();

        var investorId =
            Guid.NewGuid();

        var routeId =
            Guid.NewGuid();

        apiClient.EnqueueResponse(
            JsonResponse(
                new FinancialDelinquencyReportResponse
                {
                    Summary =
                        new FinancialDelinquencyReportSummaryResponse
                        {
                            LoansCount = 3,
                            DelinquencyRate = 12.5m
                        }
                }));

        var service =
            new FinancialReportsWebService(
                apiClient);

        var result =
            await service.GetDelinquencyAsync(
                investorId,
                routeId);

        Assert.Equal(
            3,
            result.Summary.LoansCount);

        AssertRequest(
            Assert.Single(
                apiClient.Requests),
            "api/reports/delinquency" +
            $"?investorId={investorId:D}" +
            $"&collectionRouteId={routeId:D}");
    }

    [Fact]
    public async Task GetCollectionsAsync_BuildsPeriodAndOptionalFilters()
    {
        var apiClient =
            new TestSanesApiClient();

        var investorId =
            Guid.NewGuid();

        var collectorId =
            Guid.NewGuid();

        var routeId =
            Guid.NewGuid();

        apiClient.EnqueueResponse(
            JsonResponse(
                new FinancialCollectionsReportResponse
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
                            22),

                    Summary =
                        new FinancialCollectionsReportSummaryResponse
                        {
                            PaymentsCount = 5,
                            CashCollected = 2500m
                        }
                }));

        var service =
            new FinancialReportsWebService(
                apiClient);

        var result =
            await service.GetCollectionsAsync(
                new DateOnly(
                    2026,
                    9,
                    1),
                new DateOnly(
                    2026,
                    9,
                    22),
                investorId,
                collectorId,
                routeId);

        Assert.Equal(
            5,
            result.Summary.PaymentsCount);

        AssertRequest(
            Assert.Single(
                apiClient.Requests),
            "api/reports/collections" +
            "?from=2026-09-01" +
            "&to=2026-09-22" +
            $"&investorId={investorId:D}" +
            $"&collectorId={collectorId:D}" +
            $"&collectionRouteId={routeId:D}");
    }

    [Fact]
    public async Task GetCollectionsAsync_WhenApiReturnsBadRequest_ThrowsApiMessage()
    {
        var apiClient =
            new TestSanesApiClient();

        apiClient.EnqueueResponse(
            JsonResponse(
                new
                {
                    message =
                        "The report period cannot exceed 366 days."
                },
                HttpStatusCode.BadRequest));

        var service =
            new FinancialReportsWebService(
                apiClient);

        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () =>
                    service.GetCollectionsAsync(
                        new DateOnly(
                            2025,
                            1,
                            1),
                        new DateOnly(
                            2026,
                            9,
                            22),
                        null,
                        null,
                        null));

        Assert.Equal(
            "The report period cannot exceed 366 days.",
            exception.Message);
    }

    [Fact]
    public async Task GetInvestorStatementAsync_WhenFound_DeserializesResponse()
    {
        var apiClient =
            new TestSanesApiClient();

        var investorId =
            Guid.NewGuid();

        apiClient.EnqueueResponse(
            JsonResponse(
                new FinancialInvestorStatementResponse
                {
                    InvestorId = investorId,
                    InvestorName = "Investor Test",
                    ActiveLoansCount = 4,
                    TotalOutstanding = 9000m
                }));

        var service =
            new FinancialReportsWebService(
                apiClient);

        var result =
            await service.GetInvestorStatementAsync(
                investorId);

        Assert.NotNull(
            result);

        Assert.Equal(
            "Investor Test",
            result!.InvestorName);

        AssertRequest(
            Assert.Single(
                apiClient.Requests),
            $"api/reports/investors/{investorId:D}");
    }

    [Fact]
    public async Task GetInvestorStatementAsync_WhenNotFound_ReturnsNull()
    {
        var apiClient =
            new TestSanesApiClient();

        var investorId =
            Guid.NewGuid();

        apiClient.EnqueueResponse(
            new HttpResponseMessage(
                HttpStatusCode.NotFound));

        var service =
            new FinancialReportsWebService(
                apiClient);

        var result =
            await service.GetInvestorStatementAsync(
                investorId);

        Assert.Null(
            result);
    }

    [Fact]
    public async Task ExportPortfolioAsync_ReturnsExcelMetadataAndContent()
    {
        var apiClient =
            new TestSanesApiClient();

        var response =
            ExcelResponse(
                "cartera-activa-20260922.xlsx",
                [1, 2, 3, 4]);

        apiClient.EnqueueResponse(
            response);

        var service =
            new FinancialReportsWebService(
                apiClient);

        var result =
            await service.ExportPortfolioAsync(
                null,
                null,
                false);

        Assert.Equal(
            "cartera-activa-20260922.xlsx",
            result.FileName);

        Assert.Equal(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            result.ContentType);

        Assert.Equal(
            new byte[] { 1, 2, 3, 4 },
            result.Content);

        AssertRequest(
            Assert.Single(
                apiClient.Requests),
            "api/reports/portfolio/export/xlsx");
    }

    [Fact]
    public async Task ExportCollectionsAsync_UsesSameFiltersAsReport()
    {
        var apiClient =
            new TestSanesApiClient();

        var collectorId =
            Guid.NewGuid();

        apiClient.EnqueueResponse(
            ExcelResponse(
                "cobros.xlsx",
                [5, 6, 7]));

        var service =
            new FinancialReportsWebService(
                apiClient);

        await service.ExportCollectionsAsync(
            new DateOnly(
                2026,
                9,
                1),
            new DateOnly(
                2026,
                9,
                22),
            null,
            collectorId,
            null);

        AssertRequest(
            Assert.Single(
                apiClient.Requests),
            "api/reports/collections/export/xlsx" +
            "?from=2026-09-01" +
            "&to=2026-09-22" +
            $"&collectorId={collectorId:D}");
    }

    [Fact]
    public async Task ExportInvestorStatementAsync_UsesInvestorEndpoint()
    {
        var apiClient =
            new TestSanesApiClient();

        var investorId =
            Guid.NewGuid();

        apiClient.EnqueueResponse(
            ExcelResponse(
                "inversionista.xlsx",
                [8, 9]));

        var service =
            new FinancialReportsWebService(
                apiClient);

        await service.ExportInvestorStatementAsync(
            investorId);

        AssertRequest(
            Assert.Single(
                apiClient.Requests),
            $"api/reports/investors/{investorId:D}/export/xlsx");
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

    private static HttpResponseMessage ExcelResponse(
        string fileName,
        byte[] content)
    {
        var response =
            new HttpResponseMessage(
                HttpStatusCode.OK);

        response.Content =
            new ByteArrayContent(
                content);

        response.Content.Headers.ContentType =
            new MediaTypeHeaderValue(
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

        response.Content.Headers.ContentDisposition =
            new ContentDispositionHeaderValue(
                "attachment")
            {
                FileName =
                    $"\"{fileName}\""
            };

        return response;
    }
}