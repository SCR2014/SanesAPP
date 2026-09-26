using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Sanes.Application.CollectionRouteSchedules.DTOs;
using Sanes.Web.Api;

namespace Sanes.Web.CollectionRouteSchedules;

public sealed class CollectionRouteSchedulesWebService
    : ICollectionRouteSchedulesWebService
{
    private readonly ISanesApiClient _apiClient;

    public CollectionRouteSchedulesWebService(
        ISanesApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<List<CollectionRouteScheduleResponse>> GetAllAsync(
        Guid collectionRouteId,
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var url =
            $"api/collection-route-schedules" +
            $"?collectionRouteId={collectionRouteId:D}";

        if (includeInactive)
        {
            url +=
                "&includeInactive=true";
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
            throw new ArgumentException(
                await ReadApiErrorAsync(
                    response,
                    cancellationToken));
        }

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<
                List<CollectionRouteScheduleResponse>>(
                    cancellationToken)
            ?? throw new InvalidOperationException(
                "Sanes.Api returned an empty collection route schedules response.");
    }

    public async Task<CollectionRouteScheduleResponse> CreateAsync(
        CreateCollectionRouteScheduleRequest request,
        CancellationToken cancellationToken = default)
    {
        using var message =
            new HttpRequestMessage(
                HttpMethod.Post,
                "api/collection-route-schedules")
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
            .ReadFromJsonAsync<
                CollectionRouteScheduleResponse>(
                    cancellationToken)
            ?? throw new InvalidOperationException(
                "Sanes.Api returned an empty collection route schedule response.");
    }

    public async Task<CollectionRouteScheduleResponse?> UpdateAsync(
        Guid scheduleId,
        Guid collectionRouteId,
        UpdateCollectionRouteScheduleRequest request,
        CancellationToken cancellationToken = default)
    {
        using var message =
            new HttpRequestMessage(
                HttpMethod.Put,
                $"api/collection-route-schedules/{scheduleId:D}" +
                $"?collectionRouteId={collectionRouteId:D}")
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
            .ReadFromJsonAsync<
                CollectionRouteScheduleResponse>(
                    cancellationToken)
            ?? throw new InvalidOperationException(
                "Sanes.Api returned an empty collection route schedule response.");
    }

    public async Task<bool> DeleteAsync(
        Guid scheduleId,
        Guid collectionRouteId,
        CancellationToken cancellationToken = default)
    {
        using var request =
            new HttpRequestMessage(
                HttpMethod.Delete,
                $"api/collection-route-schedules/{scheduleId:D}" +
                $"?collectionRouteId={collectionRouteId:D}");

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

    public async Task<CollectionRouteScheduleResponse?> ReactivateAsync(
        Guid scheduleId,
        Guid collectionRouteId,
        CancellationToken cancellationToken = default)
    {
        using var request =
            new HttpRequestMessage(
                HttpMethod.Patch,
                $"api/collection-route-schedules/{scheduleId:D}/reactivate" +
                $"?collectionRouteId={collectionRouteId:D}")
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
            .ReadFromJsonAsync<
                CollectionRouteScheduleResponse>(
                    cancellationToken)
            ?? throw new InvalidOperationException(
                "Sanes.Api returned an empty collection route schedule response.");
    }

    private static async Task<string> ReadApiErrorAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var content =
            await response.Content
                .ReadAsStringAsync(
                    cancellationToken);

        if (string.IsNullOrWhiteSpace(
            content))
        {
            return "La operación solicitada no es válida.";
        }

        try
        {
            using var document =
                JsonDocument.Parse(
                    content);

            var root =
                document.RootElement;

            if (root.ValueKind ==
                JsonValueKind.String)
            {
                return root.GetString()
                    ?? "La operación solicitada no es válida.";
            }

            if (root.TryGetProperty(
                    "message",
                    out var message) &&
                message.ValueKind ==
                    JsonValueKind.String)
            {
                return message.GetString()
                    ?? "La operación solicitada no es válida.";
            }

            if (root.TryGetProperty(
                    "title",
                    out var title) &&
                title.ValueKind ==
                    JsonValueKind.String)
            {
                return title.GetString()
                    ?? "La operación solicitada no es válida.";
            }
        }
        catch (JsonException)
        {
            return content.Trim('"');
        }

        return content.Trim('"');
    }
}