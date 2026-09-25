using System.Net;
using System.Net.Http.Json;
using Sanes.Application.CollectionRoutes.DTOs;
using Sanes.Web.Api;

namespace Sanes.Web.CollectionRoutes;

public sealed class CollectionRoutesWebService
    : ICollectionRoutesWebService
{
    private readonly ISanesApiClient _apiClient;

    public CollectionRoutesWebService(
        ISanesApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<List<CollectionRouteResponse>> GetAllAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var url =
            includeInactive
                ? "api/collection-routes?includeInactive=true"
                : "api/collection-routes";

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
                List<CollectionRouteResponse>>(
                    cancellationToken)
            ?? throw new InvalidOperationException(
                "Sanes.Api returned an empty collection routes response.");
    }

    public async Task<CollectionRouteResponse> CreateAsync(
        CreateCollectionRouteRequest request,
        CancellationToken cancellationToken = default)
    {
        using var message =
            new HttpRequestMessage(
                HttpMethod.Post,
                "api/collection-routes")
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
            .ReadFromJsonAsync<CollectionRouteResponse>(
                cancellationToken)
            ?? throw new InvalidOperationException(
                "Sanes.Api returned an empty collection route response.");
    }

    public async Task<CollectionRouteResponse?> UpdateAsync(
        Guid routeId,
        UpdateCollectionRouteRequest request,
        CancellationToken cancellationToken = default)
    {
        using var message =
            new HttpRequestMessage(
                HttpMethod.Put,
                $"api/collection-routes/{routeId:D}")
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
            .ReadFromJsonAsync<CollectionRouteResponse>(
                cancellationToken)
            ?? throw new InvalidOperationException(
                "Sanes.Api returned an empty collection route response.");
    }

    public async Task<bool> DeleteAsync(
        Guid routeId,
        CancellationToken cancellationToken = default)
    {
        using var request =
            new HttpRequestMessage(
                HttpMethod.Delete,
                $"api/collection-routes/{routeId:D}");

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

    public async Task<CollectionRouteResponse?> ReactivateAsync(
        Guid routeId,
        CancellationToken cancellationToken = default)
    {
        using var request =
            new HttpRequestMessage(
                HttpMethod.Patch,
                $"api/collection-routes/{routeId:D}/reactivate")
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
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<CollectionRouteResponse>(
                cancellationToken)
            ?? throw new InvalidOperationException(
                "Sanes.Api returned an empty collection route response.");
    }

    public async Task<bool> ReorderClientsAsync(
        Guid routeId,
        IReadOnlyCollection<Guid> clientIds,
        CancellationToken cancellationToken = default)
    {
        var payload =
            new ReorderCollectionRouteRequest
            {
                ClientIds =
                    clientIds.ToList()
            };

        using var message =
            new HttpRequestMessage(
                HttpMethod.Put,
                $"api/collection-routes/{routeId:D}/clients/order")
            {
                Content =
                    JsonContent.Create(
                        payload)
            };

        using var response =
            await _apiClient.SendAsync(
                message,
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

    public async Task<bool> OptimizeAsync(
        Guid routeId,
        OptimizeCollectionRouteRequest request,
        CancellationToken cancellationToken = default)
    {
        using var message =
            new HttpRequestMessage(
                HttpMethod.Post,
                $"api/collection-routes/{routeId:D}/optimize")
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

    private static async Task<string> ReadApiErrorAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var content =
            await response.Content
                .ReadAsStringAsync(
                    cancellationToken);

        if (string.IsNullOrWhiteSpace(content))
        {
            return "La operación solicitada no es válida.";
        }

        try
        {
            using var document =
                System.Text.Json.JsonDocument.Parse(
                    content);

            var root =
                document.RootElement;

            if (root.ValueKind ==
                System.Text.Json.JsonValueKind.String)
            {
                return root.GetString()
                    ?? "La operación solicitada no es válida.";
            }

            if (root.TryGetProperty(
                    "message",
                    out var message) &&
                message.ValueKind ==
                    System.Text.Json.JsonValueKind.String)
            {
                return message.GetString()
                    ?? "La operación solicitada no es válida.";
            }

            if (root.TryGetProperty(
                    "title",
                    out var title) &&
                title.ValueKind ==
                    System.Text.Json.JsonValueKind.String)
            {
                return title.GetString()
                    ?? "La operación solicitada no es válida.";
            }
        }
        catch (System.Text.Json.JsonException)
        {
            return content.Trim('"');
        }

        return content.Trim('"');
    }
}