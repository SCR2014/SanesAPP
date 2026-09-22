using System.Net;
using System.Net.Http.Json;
using Sanes.Application.CollectionRoutes.DTOs;
using Sanes.Application.FinancialReports.DTOs;
using Sanes.Application.Investors.DTOs;
using Sanes.Application.Tenants.DTOs;
using Sanes.Web.Api;
using Sanes.Application.AppUsers.DTOs;
using Sanes.Domain.Enums;

namespace Sanes.Web.FinancialReports;

public sealed class FinancialReportsWebService
    : IFinancialReportsWebService
{
    private readonly ISanesApiClient _apiClient;

    public FinancialReportsWebService(
        ISanesApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<FinancialReportsSetup> GetSetupAsync(
        CancellationToken cancellationToken = default)
    {
        var tenant =
            await GetAsync<TenantDto>(
                "api/tenants/me",
                cancellationToken);

        var investors =
            await GetAsync<List<InvestorResponse>>(
                "api/investors",
                cancellationToken);

        var routes =
            await GetAsync<List<CollectionRouteResponse>>(
                "api/collection-routes",
                cancellationToken);

        var collectors =
            await GetAsync<List<AppUserResponse>>(
                $"api/app-users?role={nameof(AppUserRole.Collector)}",
                cancellationToken);

        return new FinancialReportsSetup
        {
            Tenant = tenant,
            Investors = investors,
            CollectionRoutes = routes,
            Collectors = collectors
        };
    }

    public async Task<FinancialPortfolioReportResponse>
        GetPortfolioAsync(
            Guid? investorId,
            Guid? collectionRouteId,
            bool overdueOnly,
            CancellationToken cancellationToken = default)
    {
        var parameters =
            new List<string>();

        if (investorId.HasValue)
        {
            parameters.Add(
                $"investorId={investorId.Value:D}");
        }

        if (collectionRouteId.HasValue)
        {
            parameters.Add(
                $"collectionRouteId={collectionRouteId.Value:D}");
        }

        if (overdueOnly)
        {
            parameters.Add(
                "overdueOnly=true");
        }

        var url =
            "api/reports/portfolio";

        if (parameters.Count > 0)
        {
            url +=
                "?" +
                string.Join(
                    "&",
                    parameters);
        }

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
                ?? "Los filtros del reporte no son válidos.");
        }

        response.EnsureSuccessStatusCode();

        var report =
            await response.Content
                .ReadFromJsonAsync<
                    FinancialPortfolioReportResponse>(
                        cancellationToken);

        return report
            ?? throw new InvalidOperationException(
                "Sanes.Api returned an empty portfolio report response.");
    }

    public async Task<FinancialDelinquencyReportResponse>
        GetDelinquencyAsync(
            Guid? investorId,
            Guid? collectionRouteId,
            CancellationToken cancellationToken = default)
    {
        var parameters =
            new List<string>();

        if (investorId.HasValue)
        {
            parameters.Add(
                $"investorId={investorId.Value:D}");
        }

        if (collectionRouteId.HasValue)
        {
            parameters.Add(
                $"collectionRouteId={collectionRouteId.Value:D}");
        }

        var url =
            "api/reports/delinquency";

        if (parameters.Count > 0)
        {
            url +=
                "?" +
                string.Join(
                    "&",
                    parameters);
        }

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
                ?? "Los filtros del reporte no son válidos.");
        }

        response.EnsureSuccessStatusCode();

        var report =
            await response.Content
                .ReadFromJsonAsync<
                    FinancialDelinquencyReportResponse>(
                        cancellationToken);

        return report
            ?? throw new InvalidOperationException(
                "Sanes.Api returned an empty delinquency report response.");
    }

    public async Task<FinancialCollectionsReportResponse>
        GetCollectionsAsync(
            DateOnly from,
            DateOnly to,
            Guid? investorId,
            Guid? collectorId,
            Guid? collectionRouteId,
            CancellationToken cancellationToken = default)
    {
        var parameters =
            new List<string>
            {
                $"from={from:yyyy-MM-dd}",
                $"to={to:yyyy-MM-dd}"
            };

        if (investorId.HasValue)
        {
            parameters.Add(
                $"investorId={investorId.Value:D}");
        }

        if (collectorId.HasValue)
        {
            parameters.Add(
                $"collectorId={collectorId.Value:D}");
        }

        if (collectionRouteId.HasValue)
        {
            parameters.Add(
                $"collectionRouteId={collectionRouteId.Value:D}");
        }

        var url =
            "api/reports/collections?" +
            string.Join("&", parameters);

        return await GetReportAsync<
            FinancialCollectionsReportResponse>(
                url,
                cancellationToken);
    }

    public async Task<FinancialInvestorStatementResponse?>
        GetInvestorStatementAsync(
            Guid investorId,
            CancellationToken cancellationToken = default)
    {
        var url =
            $"api/reports/investors/{investorId:D}";

        using var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                url);

        using var response =
            await _apiClient.SendAsync(
                request,
                cancellationToken);

        if (response.StatusCode ==
            HttpStatusCode.NotFound)
        {
            return null;
        }

        if (response.StatusCode ==
            HttpStatusCode.BadRequest)
        {
            var error =
                await response.Content
                    .ReadFromJsonAsync<ApiErrorResponse>(
                        cancellationToken);

            throw new ArgumentException(
                error?.Message
                ?? "El inversionista seleccionado no es válido.");
        }

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<
                FinancialInvestorStatementResponse>(
                    cancellationToken)
            ?? throw new InvalidOperationException(
                "Sanes.Api returned an empty investor statement response.");
    }

    public Task<FinancialReportDownload>
        ExportPortfolioAsync(
            Guid? investorId,
            Guid? collectionRouteId,
            bool overdueOnly,
            CancellationToken cancellationToken = default)
    {
        var parameters =
            new List<string>();

        if (investorId.HasValue)
        {
            parameters.Add(
                $"investorId={investorId.Value:D}");
        }

        if (collectionRouteId.HasValue)
        {
            parameters.Add(
                $"collectionRouteId={collectionRouteId.Value:D}");
        }

        if (overdueOnly)
        {
            parameters.Add(
                "overdueOnly=true");
        }

        var url =
            "api/reports/portfolio/export/xlsx";

        if (parameters.Count > 0)
        {
            url +=
                "?" +
                string.Join("&", parameters);
        }

        return DownloadAsync(
            url,
            cancellationToken);
    }

    public Task<FinancialReportDownload>
        ExportDelinquencyAsync(
            Guid? investorId,
            Guid? collectionRouteId,
            CancellationToken cancellationToken = default)
    {
        var parameters =
            new List<string>();

        if (investorId.HasValue)
        {
            parameters.Add(
                $"investorId={investorId.Value:D}");
        }

        if (collectionRouteId.HasValue)
        {
            parameters.Add(
                $"collectionRouteId={collectionRouteId.Value:D}");
        }

        var url =
            "api/reports/delinquency/export/xlsx";

        if (parameters.Count > 0)
        {
            url +=
                "?" +
                string.Join("&", parameters);
        }

        return DownloadAsync(
            url,
            cancellationToken);
    }

    public Task<FinancialReportDownload>
        ExportCollectionsAsync(
            DateOnly from,
            DateOnly to,
            Guid? investorId,
            Guid? collectorId,
            Guid? collectionRouteId,
            CancellationToken cancellationToken = default)
    {
        var parameters =
            new List<string>
            {
                $"from={from:yyyy-MM-dd}",
                $"to={to:yyyy-MM-dd}"
            };

        if (investorId.HasValue)
        {
            parameters.Add(
                $"investorId={investorId.Value:D}");
        }

        if (collectorId.HasValue)
        {
            parameters.Add(
                $"collectorId={collectorId.Value:D}");
        }

        if (collectionRouteId.HasValue)
        {
            parameters.Add(
                $"collectionRouteId={collectionRouteId.Value:D}");
        }

        var url =
            "api/reports/collections/export/xlsx?" +
            string.Join("&", parameters);

        return DownloadAsync(
            url,
            cancellationToken);
    }

    public Task<FinancialReportDownload>
        ExportInvestorStatementAsync(
            Guid investorId,
            CancellationToken cancellationToken = default)
    {
        return DownloadAsync(
            $"api/reports/investors/{investorId:D}/export/xlsx",
            cancellationToken);
    }

    private async Task<T> GetAsync<T>(
        string url,
        CancellationToken cancellationToken)
    {
        using var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                url);

        using var response =
            await _apiClient.SendAsync(
                request,
                cancellationToken);

        response.EnsureSuccessStatusCode();

        var result =
            await response.Content
                .ReadFromJsonAsync<T>(
                    cancellationToken);

        return result
            ?? throw new InvalidOperationException(
                $"Sanes.Api returned an empty response for {url}.");
    }

    private sealed class ApiErrorResponse
    {
        public string? Message { get; set; }
    }

    private async Task<T> GetReportAsync<T>(
        string url,
        CancellationToken cancellationToken)
    {
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
                ?? "Los parámetros del reporte no son válidos.");
        }

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<T>(
                cancellationToken)
            ?? throw new InvalidOperationException(
                $"Sanes.Api returned an empty response for {url}.");
    }

    private async Task<FinancialReportDownload>
        DownloadAsync(
            string url,
            CancellationToken cancellationToken)
    {
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
                ?? "Los parámetros de exportación no son válidos.");
        }

        if (response.StatusCode ==
            HttpStatusCode.NotFound)
        {
            throw new InvalidOperationException(
                "No se encontró información para exportar.");
        }

        response.EnsureSuccessStatusCode();

        var content =
            await response.Content
                .ReadAsByteArrayAsync(
                    cancellationToken);

        var contentType =
            response.Content.Headers
                .ContentType?
                .MediaType
            ?? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        var fileName =
            response.Content.Headers
                .ContentDisposition?
                .FileNameStar
            ?? response.Content.Headers
                .ContentDisposition?
                .FileName
            ?? "reporte.xlsx";

        fileName =
            fileName.Trim('"');

        return new FinancialReportDownload
        {
            Content = content,
            ContentType = contentType,
            FileName = fileName
        };
    }
}