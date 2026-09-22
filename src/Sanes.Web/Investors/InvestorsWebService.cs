using System.Net;
using System.Net.Http.Json;
using Sanes.Application.Investors.DTOs;
using Sanes.Web.Api;

namespace Sanes.Web.Investors;

public sealed class InvestorsWebService
    : IInvestorsWebService
{
    private readonly ISanesApiClient _apiClient;

    public InvestorsWebService(
        ISanesApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<List<InvestorResponse>> GetAllAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var url =
            includeInactive
                ? "api/investors?includeInactive=true"
                : "api/investors";

        using var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                url);

        using var response =
            await _apiClient.SendAsync(
                request,
                cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<List<InvestorResponse>>(
                cancellationToken)
            ?? throw new InvalidOperationException(
                "Sanes.Api returned an empty investors response.");
    }

    public async Task<InvestorResponse> CreateAsync(
        CreateInvestorRequest request,
        CancellationToken cancellationToken = default)
    {
        using var message =
            new HttpRequestMessage(
                HttpMethod.Post,
                "api/investors")
            {
                Content =
                    JsonContent.Create(request)
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
            .ReadFromJsonAsync<InvestorResponse>(
                cancellationToken)
            ?? throw new InvalidOperationException(
                "Sanes.Api returned an empty investor response.");
    }

    public async Task<InvestorResponse?> UpdateAsync(
        Guid investorId,
        UpdateInvestorRequest request,
        CancellationToken cancellationToken = default)
    {
        using var message =
            new HttpRequestMessage(
                HttpMethod.Put,
                $"api/investors/{investorId:D}")
            {
                Content =
                    JsonContent.Create(request)
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
            .ReadFromJsonAsync<InvestorResponse>(
                cancellationToken)
            ?? throw new InvalidOperationException(
                "Sanes.Api returned an empty investor response.");
    }

    public async Task<bool> DeleteAsync(
        Guid investorId,
        CancellationToken cancellationToken = default)
    {
        using var request =
            new HttpRequestMessage(
                HttpMethod.Delete,
                $"api/investors/{investorId:D}");

        using var response =
            await _apiClient.SendAsync(
                request,
                cancellationToken);

        if (response.StatusCode ==
            HttpStatusCode.NotFound)
        {
            return false;
        }

        response.EnsureSuccessStatusCode();

        return true;
    }

    public async Task<bool> ReactivateAsync(
        Guid investorId,
        CancellationToken cancellationToken = default)
    {
        using var request =
            new HttpRequestMessage(
                HttpMethod.Patch,
                $"api/investors/{investorId:D}/reactivate")
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

        response.EnsureSuccessStatusCode();

        return true;
    }

    private static async Task<string> ReadApiErrorAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var error =
            await response.Content
                .ReadFromJsonAsync<ApiErrorResponse>(
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
                    .SelectMany(x => x.Value)
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

        return "La operación solicitada no es válida.";
    }

    private sealed class ApiErrorResponse
    {
        public string? Message { get; set; }

        public string? Title { get; set; }

        public Dictionary<string, string[]>?
            Errors { get; set; }
    }
}