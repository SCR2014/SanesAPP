using System.Net;
using System.Net.Http.Json;
using Sanes.Application.Clients.DTOs;
using Sanes.Application.CollectionRoutes.DTOs;
using Sanes.Web.Api;

namespace Sanes.Web.Clients;

public sealed class ClientsWebService
    : IClientsWebService
{
    private readonly ISanesApiClient _apiClient;

    public ClientsWebService(
        ISanesApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<List<ClientResponse>> GetAllAsync(
        Guid? collectionRouteId = null,
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var query =
            new List<string>();

        if (collectionRouteId.HasValue)
        {
            query.Add(
                $"collectionRouteId=" +
                $"{collectionRouteId.Value:D}");
        }

        if (includeInactive)
        {
            query.Add(
                "includeInactive=true");
        }

        var url =
            query.Count == 0
                ? "api/clients"
                : "api/clients?" +
                  string.Join(
                      "&",
                      query);

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
            .ReadFromJsonAsync<
                List<ClientResponse>>(
                    cancellationToken)
            ?? throw new InvalidOperationException(
                "Sanes.Api returned an empty clients response.");
    }

    public async Task<List<CollectionRouteResponse>>
        GetRoutesAsync(
            CancellationToken cancellationToken = default)
    {
        using var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                "api/collection-routes");

        using var response =
            await _apiClient.SendAsync(
                request,
                cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<
                List<CollectionRouteResponse>>(
                    cancellationToken)
            ?? throw new InvalidOperationException(
                "Sanes.Api returned an empty collection routes response.");
    }

    public async Task<ClientResponse> CreateAsync(
        CreateClientRequest request,
        CancellationToken cancellationToken = default)
    {
        using var message =
            new HttpRequestMessage(
                HttpMethod.Post,
                "api/clients")
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
            .ReadFromJsonAsync<ClientResponse>(
                cancellationToken)
            ?? throw new InvalidOperationException(
                "Sanes.Api returned an empty client response.");
    }

    public async Task<ClientResponse?> UpdateAsync(
        Guid clientId,
        UpdateClientRequest request,
        CancellationToken cancellationToken = default)
    {
        using var message =
            new HttpRequestMessage(
                HttpMethod.Put,
                $"api/clients/{clientId:D}")
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
            .ReadFromJsonAsync<ClientResponse>(
                cancellationToken)
            ?? throw new InvalidOperationException(
                "Sanes.Api returned an empty client response.");
    }

    public async Task<bool> DeleteAsync(
        Guid clientId,
        CancellationToken cancellationToken = default)
    {
        using var request =
            new HttpRequestMessage(
                HttpMethod.Delete,
                $"api/clients/{clientId:D}");

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
        Guid clientId,
        CancellationToken cancellationToken = default)
    {
        using var request =
            new HttpRequestMessage(
                HttpMethod.Patch,
                $"api/clients/{clientId:D}/reactivate")
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
        try
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
                        .SelectMany(x =>
                            x.Value)
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

        return string.IsNullOrWhiteSpace(content)
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