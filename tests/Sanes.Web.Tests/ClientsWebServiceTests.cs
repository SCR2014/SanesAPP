using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Sanes.Application.Clients.DTOs;
using Sanes.Application.CollectionRoutes.DTOs;
using Sanes.Web.Clients;

namespace Sanes.Web.Tests;

public class ClientsWebServiceTests
{
    [Fact]
    public async Task GetAllAsync_WithoutFilters_UsesBaseEndpoint()
    {
        var apiClient =
            new TestSanesApiClient();

        apiClient.EnqueueResponse(
            JsonResponse(
                new List<ClientResponse>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        FirstName = "Carlos",
                        Phone = "8095551234",
                        IsActive = true
                    }
                }));

        var service =
            new ClientsWebService(
                apiClient);

        var result =
            await service.GetAllAsync();

        Assert.Single(result);

        AssertRequest(
            Assert.Single(
                apiClient.Requests),
            HttpMethod.Get,
            "api/clients");
    }

    [Fact]
    public async Task GetAllAsync_WithRouteAndInactive_BuildsExpectedUrl()
    {
        var apiClient =
            new TestSanesApiClient();

        var routeId =
            Guid.NewGuid();

        apiClient.EnqueueResponse(
            JsonResponse(
                new List<ClientResponse>()));

        var service =
            new ClientsWebService(
                apiClient);

        await service.GetAllAsync(
            routeId,
            includeInactive: true);

        AssertRequest(
            Assert.Single(
                apiClient.Requests),
            HttpMethod.Get,
            $"api/clients?collectionRouteId={routeId:D}" +
            "&includeInactive=true");
    }

    [Fact]
    public async Task GetRoutesAsync_UsesCollectionRoutesEndpoint()
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
                        IsActive = true
                    }
                }));

        var service =
            new ClientsWebService(
                apiClient);

        var routes =
            await service.GetRoutesAsync();

        Assert.Single(routes);

        Assert.Equal(
            "Ruta Centro",
            routes[0].Name);

        AssertRequest(
            Assert.Single(
                apiClient.Requests),
            HttpMethod.Get,
            "api/collection-routes");
    }

    [Fact]
    public async Task CreateAsync_SendsExpectedRequest()
    {
        var apiClient =
            new TestSanesApiClient();

        var clientId =
            Guid.NewGuid();

        var routeId =
            Guid.NewGuid();

        apiClient.EnqueueResponse(
            JsonResponse(
                new ClientResponse
                {
                    Id = clientId,
                    FirstName = "Carlos",
                    LastName = "Rodriguez",
                    Phone = "8095551234",
                    CollectionRouteId = routeId,
                    IsActive = true
                },
                HttpStatusCode.Created));

        var service =
            new ClientsWebService(
                apiClient);

        var result =
            await service.CreateAsync(
                new CreateClientRequest
                {
                    FirstName = "Carlos",
                    LastName = "Rodriguez",
                    Phone = "8095551234",
                    SecondaryPhone = "8295551234",
                    IdentificationType = "Cedula",
                    Identification = "00112345678",
                    SocialNumber = "SS-001",
                    Address = "Santiago",
                    Latitude = 19.45m,
                    Longitude = -70.69m,
                    CollectionRouteId = routeId,
                    Notes = "Cliente Web"
                });

        Assert.Equal(
            clientId,
            result.Id);

        var recorded =
            Assert.Single(
                apiClient.Requests);

        AssertRequest(
            recorded,
            HttpMethod.Post,
            "api/clients");

        Assert.NotNull(
            recorded.Body);

        using var document =
            JsonDocument.Parse(
                recorded.Body!);

        var root =
            document.RootElement;

        Assert.Equal(
            "Carlos",
            root.GetProperty("firstName")
                .GetString());

        Assert.Equal(
            "8095551234",
            root.GetProperty("phone")
                .GetString());

        Assert.Equal(
            routeId,
            root.GetProperty("collectionRouteId")
                .GetGuid());

        Assert.False(
            root.TryGetProperty(
                "tenantId",
                out _));
    }

    [Fact]
    public async Task CreateAsync_WhenBadRequest_ThrowsApiMessage()
    {
        var apiClient =
            new TestSanesApiClient();

        apiClient.EnqueueResponse(
            JsonResponse(
                new
                {
                    message =
                        "A client with the same identification already exists for this tenant."
                },
                HttpStatusCode.BadRequest));

        var service =
            new ClientsWebService(
                apiClient);

        var exception =
            await Assert.ThrowsAsync<
                ArgumentException>(
                () =>
                    service.CreateAsync(
                        new CreateClientRequest
                        {
                            FirstName =
                                "Carlos",
                            Phone =
                                "8095551234",
                            Identification =
                                "00112345678"
                        }));

        Assert.Equal(
            "A client with the same identification already exists for this tenant.",
            exception.Message);
    }

    [Fact]
    public async Task UpdateAsync_SendsExpectedRequest()
    {
        var apiClient =
            new TestSanesApiClient();

        var clientId =
            Guid.NewGuid();

        var routeId =
            Guid.NewGuid();

        apiClient.EnqueueResponse(
            JsonResponse(
                new ClientResponse
                {
                    Id = clientId,
                    FirstName =
                        "Carlos Actualizado",
                    Phone =
                        "8095559999",
                    CollectionRouteId =
                        routeId,
                    IsActive = true
                }));

        var service =
            new ClientsWebService(
                apiClient);

        var result =
            await service.UpdateAsync(
                clientId,
                new UpdateClientRequest
                {
                    FirstName =
                        "Carlos Actualizado",
                    Phone =
                        "8095559999",
                    CollectionRouteId =
                        routeId
                });

        Assert.NotNull(result);

        var recorded =
            Assert.Single(
                apiClient.Requests);

        AssertRequest(
            recorded,
            HttpMethod.Put,
            $"api/clients/{clientId:D}");

        Assert.NotNull(
            recorded.Body);

        using var document =
            JsonDocument.Parse(
                recorded.Body!);

        var root =
            document.RootElement;

        Assert.Equal(
            "Carlos Actualizado",
            root.GetProperty("firstName")
                .GetString());

        Assert.False(
            root.TryGetProperty(
                "tenantId",
                out _));
    }

    [Fact]
    public async Task UpdateAsync_WhenNotFound_ReturnsNull()
    {
        var apiClient =
            new TestSanesApiClient();

        var clientId =
            Guid.NewGuid();

        apiClient.EnqueueResponse(
            new HttpResponseMessage(
                HttpStatusCode.NotFound));

        var service =
            new ClientsWebService(
                apiClient);

        var result =
            await service.UpdateAsync(
                clientId,
                new UpdateClientRequest
                {
                    FirstName =
                        "Cliente",
                    Phone =
                        "8095551234"
                });

        Assert.Null(result);

        AssertRequest(
            Assert.Single(
                apiClient.Requests),
            HttpMethod.Put,
            $"api/clients/{clientId:D}");
    }

    [Fact]
    public async Task DeleteAsync_UsesExpectedEndpoint()
    {
        var apiClient =
            new TestSanesApiClient();

        var clientId =
            Guid.NewGuid();

        apiClient.EnqueueResponse(
            new HttpResponseMessage(
                HttpStatusCode.NoContent));

        var service =
            new ClientsWebService(
                apiClient);

        var result =
            await service.DeleteAsync(
                clientId);

        Assert.True(result);

        AssertRequest(
            Assert.Single(
                apiClient.Requests),
            HttpMethod.Delete,
            $"api/clients/{clientId:D}");
    }

    [Fact]
    public async Task DeleteAsync_WhenNotFound_ReturnsFalse()
    {
        var apiClient =
            new TestSanesApiClient();

        apiClient.EnqueueResponse(
            new HttpResponseMessage(
                HttpStatusCode.NotFound));

        var service =
            new ClientsWebService(
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

        var clientId =
            Guid.NewGuid();

        apiClient.EnqueueResponse(
            new HttpResponseMessage(
                HttpStatusCode.NoContent));

        var service =
            new ClientsWebService(
                apiClient);

        var result =
            await service.ReactivateAsync(
                clientId);

        Assert.True(result);

        AssertRequest(
            Assert.Single(
                apiClient.Requests),
            HttpMethod.Patch,
            $"api/clients/{clientId:D}/reactivate");
    }

    [Fact]
    public async Task ReactivateAsync_WhenNotFound_ReturnsFalse()
    {
        var apiClient =
            new TestSanesApiClient();

        apiClient.EnqueueResponse(
            new HttpResponseMessage(
                HttpStatusCode.NotFound));

        var service =
            new ClientsWebService(
                apiClient);

        var result =
            await service.ReactivateAsync(
                Guid.NewGuid());

        Assert.False(result);
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