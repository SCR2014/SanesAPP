using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Sanes.Application.CollectionRoutes.DTOs;
using Sanes.Domain.Enums;
using Sanes.Web.CollectionRoutes;

namespace Sanes.Web.Tests;

public class CollectionRoutesWebServiceTests
{
    [Fact]
    public async Task GetAllAsync_Default_UsesBaseEndpoint()
    {
        var apiClient =
            new TestSanesApiClient();

        apiClient.EnqueueResponse(
            JsonResponse(
                new List<CollectionRouteResponse>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        Name = "Ruta Centro",
                        OrderMode =
                            CollectionRouteOrderMode.Manual,
                        IsActive = true
                    }
                }));

        var service =
            new CollectionRoutesWebService(
                apiClient);

        var result =
            await service.GetAllAsync();

        Assert.Single(result);

        AssertRequest(
            Assert.Single(apiClient.Requests),
            HttpMethod.Get,
            "api/collection-routes");
    }

    [Fact]
    public async Task GetAllAsync_IncludeInactive_UsesExpectedQuery()
    {
        var apiClient =
            new TestSanesApiClient();

        apiClient.EnqueueResponse(
            JsonResponse(
                new List<CollectionRouteResponse>()));

        var service =
            new CollectionRoutesWebService(
                apiClient);

        await service.GetAllAsync(
            includeInactive: true);

        AssertRequest(
            Assert.Single(apiClient.Requests),
            HttpMethod.Get,
            "api/collection-routes?includeInactive=true");
    }

    [Fact]
    public async Task CreateAsync_SendsExpectedRequest()
    {
        var apiClient =
            new TestSanesApiClient();

        var routeId =
            Guid.NewGuid();

        apiClient.EnqueueResponse(
            JsonResponse(
                new CollectionRouteResponse
                {
                    Id = routeId,
                    Name = "Ruta Santiago",
                    Description = "Ruta principal",
                    OrderMode =
                        CollectionRouteOrderMode.Manual,
                    IsActive = true
                },
                HttpStatusCode.Created));

        var service =
            new CollectionRoutesWebService(
                apiClient);

        var result =
            await service.CreateAsync(
                new CreateCollectionRouteRequest
                {
                    Name = "Ruta Santiago",
                    Description = "Ruta principal",
                    OrderMode =
                        CollectionRouteOrderMode.Manual
                });

        Assert.Equal(
            routeId,
            result.Id);

        var recorded =
            Assert.Single(apiClient.Requests);

        AssertRequest(
            recorded,
            HttpMethod.Post,
            "api/collection-routes");

        Assert.NotNull(recorded.Body);

        using var document =
            JsonDocument.Parse(
                recorded.Body!);

        var root =
            document.RootElement;

        Assert.Equal(
            "Ruta Santiago",
            root.GetProperty("name")
                .GetString());

        Assert.Equal(
            (int)CollectionRouteOrderMode.Manual,
            root.GetProperty("orderMode")
                .GetInt32());

        Assert.False(
            root.TryGetProperty(
                "tenantId",
                out _));
    }

    [Fact]
    public async Task CreateAsync_BadRequest_ThrowsApiMessage()
    {
        var apiClient =
            new TestSanesApiClient();

        apiClient.EnqueueResponse(
            JsonResponse(
                "Name is required.",
                HttpStatusCode.BadRequest));

        var service =
            new CollectionRoutesWebService(
                apiClient);

        var exception =
            await Assert.ThrowsAsync<
                ArgumentException>(
                () =>
                    service.CreateAsync(
                        new CreateCollectionRouteRequest
                        {
                            Name = " "
                        }));

        Assert.Equal(
            "Name is required.",
            exception.Message);
    }

    [Fact]
    public async Task UpdateAsync_SendsExpectedRequest()
    {
        var apiClient =
            new TestSanesApiClient();

        var routeId =
            Guid.NewGuid();

        apiClient.EnqueueResponse(
            JsonResponse(
                new CollectionRouteResponse
                {
                    Id = routeId,
                    Name = "Ruta Actualizada",
                    OrderMode =
                        CollectionRouteOrderMode.Automatic,
                    IsActive = true
                }));

        var service =
            new CollectionRoutesWebService(
                apiClient);

        var result =
            await service.UpdateAsync(
                routeId,
                new UpdateCollectionRouteRequest
                {
                    Name = "Ruta Actualizada",
                    Description = "Actualizada",
                    OrderMode =
                        CollectionRouteOrderMode.Automatic
                });

        Assert.NotNull(result);

        var recorded =
            Assert.Single(apiClient.Requests);

        AssertRequest(
            recorded,
            HttpMethod.Put,
            $"api/collection-routes/{routeId:D}");

        Assert.NotNull(recorded.Body);

        using var document =
            JsonDocument.Parse(
                recorded.Body!);

        Assert.False(
            document.RootElement.TryGetProperty(
                "tenantId",
                out _));
    }

    [Fact]
    public async Task UpdateAsync_NotFound_ReturnsNull()
    {
        var apiClient =
            new TestSanesApiClient();

        var routeId =
            Guid.NewGuid();

        apiClient.EnqueueResponse(
            new HttpResponseMessage(
                HttpStatusCode.NotFound));

        var service =
            new CollectionRoutesWebService(
                apiClient);

        var result =
            await service.UpdateAsync(
                routeId,
                new UpdateCollectionRouteRequest
                {
                    Name = "Ruta"
                });

        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_UsesExpectedEndpoint()
    {
        var apiClient =
            new TestSanesApiClient();

        var routeId =
            Guid.NewGuid();

        apiClient.EnqueueResponse(
            new HttpResponseMessage(
                HttpStatusCode.NoContent));

        var service =
            new CollectionRoutesWebService(
                apiClient);

        var result =
            await service.DeleteAsync(
                routeId);

        Assert.True(result);

        AssertRequest(
            Assert.Single(apiClient.Requests),
            HttpMethod.Delete,
            $"api/collection-routes/{routeId:D}");
    }

    [Fact]
    public async Task DeleteAsync_NotFound_ReturnsFalse()
    {
        var apiClient =
            new TestSanesApiClient();

        apiClient.EnqueueResponse(
            new HttpResponseMessage(
                HttpStatusCode.NotFound));

        var service =
            new CollectionRoutesWebService(
                apiClient);

        var result =
            await service.DeleteAsync(
                Guid.NewGuid());

        Assert.False(result);
    }

    [Fact]
    public async Task ReactivateAsync_UsesExpectedEndpoint()
    {
        var apiClient =
            new TestSanesApiClient();

        var routeId =
            Guid.NewGuid();

        apiClient.EnqueueResponse(
            JsonResponse(
                new CollectionRouteResponse
                {
                    Id = routeId,
                    Name = "Ruta",
                    IsActive = true
                }));

        var service =
            new CollectionRoutesWebService(
                apiClient);

        var result =
            await service.ReactivateAsync(
                routeId);

        Assert.NotNull(result);
        Assert.True(result.IsActive);

        AssertRequest(
            Assert.Single(apiClient.Requests),
            HttpMethod.Patch,
            $"api/collection-routes/{routeId:D}/reactivate");
    }

    [Fact]
    public async Task ReactivateAsync_NotFound_ReturnsNull()
    {
        var apiClient =
            new TestSanesApiClient();

        apiClient.EnqueueResponse(
            new HttpResponseMessage(
                HttpStatusCode.NotFound));

        var service =
            new CollectionRoutesWebService(
                apiClient);

        var result =
            await service.ReactivateAsync(
                Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task ReorderClientsAsync_SendsExpectedRequest()
    {
        var apiClient =
            new TestSanesApiClient();

        var routeId =
            Guid.NewGuid();

        var client1 =
            Guid.NewGuid();

        var client2 =
            Guid.NewGuid();

        apiClient.EnqueueResponse(
            new HttpResponseMessage(
                HttpStatusCode.NoContent));

        var service =
            new CollectionRoutesWebService(
                apiClient);

        var result =
            await service.ReorderClientsAsync(
                routeId,
                new[]
                {
                    client2,
                    client1
                });

        Assert.True(result);

        var recorded =
            Assert.Single(apiClient.Requests);

        AssertRequest(
            recorded,
            HttpMethod.Put,
            $"api/collection-routes/{routeId:D}/clients/order");

        Assert.NotNull(recorded.Body);

        using var document =
            JsonDocument.Parse(
                recorded.Body!);

        var clientIds =
            document.RootElement
                .GetProperty("clientIds")
                .EnumerateArray()
                .Select(x => x.GetGuid())
                .ToArray();

        Assert.Equal(
            new[]
            {
                client2,
                client1
            },
            clientIds);

        Assert.False(
            document.RootElement.TryGetProperty(
                "tenantId",
                out _));
    }

    [Fact]
    public async Task ReorderClientsAsync_BadRequest_ThrowsApiMessage()
    {
        var apiClient =
            new TestSanesApiClient();

        apiClient.EnqueueResponse(
            JsonResponse(
                new
                {
                    message =
                        "Clients can only be manually reordered when the collection route is in Manual order mode."
                },
                HttpStatusCode.BadRequest));

        var service =
            new CollectionRoutesWebService(
                apiClient);

        var exception =
            await Assert.ThrowsAsync<
                ArgumentException>(
                () =>
                    service.ReorderClientsAsync(
                        Guid.NewGuid(),
                        Array.Empty<Guid>()));

        Assert.Contains(
            "Manual order mode",
            exception.Message);
    }

    [Fact]
    public async Task OptimizeAsync_SendsExpectedRequest()
    {
        var apiClient =
            new TestSanesApiClient();

        var routeId =
            Guid.NewGuid();

        apiClient.EnqueueResponse(
            new HttpResponseMessage(
                HttpStatusCode.NoContent));

        var service =
            new CollectionRoutesWebService(
                apiClient);

        var result =
            await service.OptimizeAsync(
                routeId,
                new OptimizeCollectionRouteRequest
                {
                    StartLatitude =
                        19.45m,
                    StartLongitude =
                        -70.70m
                });

        Assert.True(result);

        var recorded =
            Assert.Single(apiClient.Requests);

        AssertRequest(
            recorded,
            HttpMethod.Post,
            $"api/collection-routes/{routeId:D}/optimize");

        Assert.NotNull(recorded.Body);

        using var document =
            JsonDocument.Parse(
                recorded.Body!);

        Assert.Equal(
            19.45m,
            document.RootElement
                .GetProperty("startLatitude")
                .GetDecimal());

        Assert.Equal(
            -70.70m,
            document.RootElement
                .GetProperty("startLongitude")
                .GetDecimal());

        Assert.False(
            document.RootElement.TryGetProperty(
                "tenantId",
                out _));
    }

    [Fact]
    public async Task OptimizeAsync_BadRequest_ThrowsApiMessage()
    {
        var apiClient =
            new TestSanesApiClient();

        apiClient.EnqueueResponse(
            JsonResponse(
                new
                {
                    message =
                        "Clients can only be automatically optimized when the collection route is in Automatic order mode."
                },
                HttpStatusCode.BadRequest));

        var service =
            new CollectionRoutesWebService(
                apiClient);

        var exception =
            await Assert.ThrowsAsync<
                ArgumentException>(
                () =>
                    service.OptimizeAsync(
                        Guid.NewGuid(),
                        new OptimizeCollectionRouteRequest()));

        Assert.Contains(
            "Automatic order mode",
            exception.Message);
    }

    private static void AssertRequest(
        TestSanesApiClient.RecordedRequest request,
        HttpMethod expectedMethod,
        string expectedUri)
    {
        Assert.Equal(
            expectedMethod,
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