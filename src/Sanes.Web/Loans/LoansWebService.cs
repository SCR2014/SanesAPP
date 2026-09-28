using System.Net;
using System.Net.Http.Json;
using Sanes.Application.Loans.DTOs;
using Sanes.Application.Tenants.DTOs;
using Sanes.Web.Api;
using Sanes.Application.Clients.DTOs;
using Sanes.Application.Investors.DTOs;

namespace Sanes.Web.Loans;

public sealed class LoansWebService
    : ILoansWebService
{
    private readonly ISanesApiClient _apiClient;

    public LoansWebService(
        ISanesApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<List<ClientResponse>> GetClientsAsync(
        CancellationToken cancellationToken = default)
    {
        using var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                "api/clients");

        using var response =
            await _apiClient.SendAsync(
                request,
                cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<
                List<ClientResponse>>(
                    cancellationToken)
            ?? throw new InvalidOperationException(
                "Sanes.Api returned an empty clients response.");
    }

    public async Task<List<InvestorResponse>> GetInvestorsAsync(
        CancellationToken cancellationToken = default)
    {
        using var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                "api/investors");

        using var response =
            await _apiClient.SendAsync(
                request,
                cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<
                List<InvestorResponse>>(
                    cancellationToken)
            ?? throw new InvalidOperationException(
                "Sanes.Api returned an empty investors response.");
    }

    public async Task<List<LoanResponse>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        using var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                "api/loans");

        using var response =
            await _apiClient.SendAsync(
                request,
                cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<
                List<LoanResponse>>(
                    cancellationToken)
            ?? throw new InvalidOperationException(
                "Sanes.Api returned an empty loans response.");
    }

    public async Task<LoanResponse?> GetByIdAsync(
        Guid loanId,
        CancellationToken cancellationToken = default)
    {
        using var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                $"api/loans/{loanId:D}");

        using var response =
            await _apiClient.SendAsync(
                request,
                cancellationToken);

        if (response.StatusCode ==
            HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<LoanResponse>(
                cancellationToken)
            ?? throw new InvalidOperationException(
                "Sanes.Api returned an empty loan response.");
    }

    public async Task<LoanFinancialSummaryResponse?>
        GetFinancialSummaryAsync(
            Guid loanId,
            CancellationToken cancellationToken = default)
    {
        using var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                $"api/loans/{loanId:D}/summary");

        using var response =
            await _apiClient.SendAsync(
                request,
                cancellationToken);

        if (response.StatusCode ==
            HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<
                LoanFinancialSummaryResponse>(
                    cancellationToken)
            ?? throw new InvalidOperationException(
                "Sanes.Api returned an empty loan summary response.");
    }

    public async Task<TenantDto> GetTenantAsync(
        CancellationToken cancellationToken = default)
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

        return await response.Content
            .ReadFromJsonAsync<TenantDto>(
                cancellationToken)
            ?? throw new InvalidOperationException(
                "Sanes.Api returned an empty tenant response.");
    }

    public async Task<LoanResponse> CreateAsync(
        CreateLoanRequest request,
        CancellationToken cancellationToken = default)
    {
        using var message =
            new HttpRequestMessage(
                HttpMethod.Post,
                "api/loans")
            {
                Content =
                    JsonContent.Create(
                        request)
            };

        using var response =
            await _apiClient.SendAsync(
                message,
                cancellationToken);

        if (response.StatusCode ==
            HttpStatusCode.BadRequest)
        {
            throw new ArgumentException(
                await ReadApiErrorAsync(
                    response,
                    cancellationToken));
        }

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<LoanResponse>(
                cancellationToken)
            ?? throw new InvalidOperationException(
                "Sanes.Api returned an empty loan response.");
    }

    public async Task<LoanResponse?> UpdateAsync(
        Guid loanId,
        UpdateLoanRequest request,
        CancellationToken cancellationToken = default)
    {
        using var message =
            new HttpRequestMessage(
                HttpMethod.Put,
                $"api/loans/{loanId:D}")
            {
                Content =
                    JsonContent.Create(
                        request)
            };

        using var response =
            await _apiClient.SendAsync(
                message,
                cancellationToken);

        if (response.StatusCode ==
            HttpStatusCode.NotFound)
        {
            return null;
        }

        if (response.StatusCode ==
            HttpStatusCode.BadRequest)
        {
            throw new ArgumentException(
                await ReadApiErrorAsync(
                    response,
                    cancellationToken));
        }

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<LoanResponse>(
                cancellationToken)
            ?? throw new InvalidOperationException(
                "Sanes.Api returned an empty loan response.");
    }

    public async Task<bool> CancelAsync(
        Guid loanId,
        CancellationToken cancellationToken = default)
    {
        using var request =
            new HttpRequestMessage(
                HttpMethod.Patch,
                $"api/loans/{loanId:D}/cancel")
            {
                Content =
                    new StringContent(
                        string.Empty)
            };

        using var response =
            await _apiClient.SendAsync(
                request,
                cancellationToken);

        if (response.StatusCode ==
            HttpStatusCode.NotFound)
        {
            return false;
        }

        if (response.StatusCode ==
            HttpStatusCode.BadRequest)
        {
            throw new ArgumentException(
                await ReadApiErrorAsync(
                    response,
                    cancellationToken));
        }

        response.EnsureSuccessStatusCode();

        return true;
    }

    private static async Task<string>
        ReadApiErrorAsync(
            HttpResponseMessage response,
            CancellationToken cancellationToken)
    {
        try
        {
            var error =
                await response.Content
                    .ReadFromJsonAsync<
                        ApiErrorResponse>(
                            cancellationToken);

            if (!string.IsNullOrWhiteSpace(
                error?.Message))
            {
                return error.Message;
            }

            if (error?.Errors is not null)
            {
                var validationError =
                    error.Errors
                        .SelectMany(
                            x => x.Value)
                        .FirstOrDefault();

                if (!string.IsNullOrWhiteSpace(
                    validationError))
                {
                    return validationError;
                }
            }

            if (!string.IsNullOrWhiteSpace(
                error?.Title))
            {
                return error.Title;
            }
        }
        catch
        {
            // Fallback to plain text below.
        }

        var content =
            await response.Content
                .ReadAsStringAsync(
                    cancellationToken);

        return string.IsNullOrWhiteSpace(
            content)
            ? "La operación solicitada no es válida."
            : content.Trim('"');
    }

    private sealed class ApiErrorResponse
    {
        public string? Message { get; set; }

        public string? Title { get; set; }

        public Dictionary<string, string[]>?
            Errors { get; set; }
    }
}