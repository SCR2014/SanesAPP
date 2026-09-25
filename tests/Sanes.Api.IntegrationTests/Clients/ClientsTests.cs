using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sanes.Api.IntegrationTests.Helpers;
using Sanes.Application.Clients.DTOs;
using Sanes.Domain.Entities;
using Sanes.Infrastructure.Persistence;

namespace Sanes.Api.IntegrationTests.Clients;

public class ClientsTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ClientsTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Create_WithValidCollectionRoute_SavesCollectionRouteId()
    {
        var context = await CreateContextAsync();

        var route =
            await CreateRouteAsync(
                context.TenantId);

        var request = new CreateClientRequest
        {
            FirstName = "Cliente",
            LastName = "Ruta",
            Phone =
                $"809{Random.Shared.Next(1000000, 9999999)}",
            CollectionRouteId = route.Id
        };

        var response =
            await context.Client.PostAsJsonAsync(
                "/api/clients",
                request);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var client =
            await response.Content
                .ReadFromJsonAsync<ClientResponse>();

        Assert.NotNull(client);

        Assert.Equal(
            context.TenantId,
            client.TenantId);

        Assert.Equal(
            route.Id,
            client.CollectionRouteId);
    }

    [Fact]
    public async Task Create_WithNonExistingCollectionRoute_ReturnsBadRequest()
    {
        var context = await CreateContextAsync();

        var request = new CreateClientRequest
        {
            FirstName = "Cliente",
            LastName = "Ruta Inexistente",
            Phone =
                $"809{Random.Shared.Next(1000000, 9999999)}",
            CollectionRouteId = Guid.NewGuid()
        };

        var response =
            await context.Client.PostAsJsonAsync(
                "/api/clients",
                request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task Create_WithCollectionRouteFromDifferentTenant_ReturnsBadRequest()
    {
        var tenant1 = await CreateContextAsync();
        var tenant2 = await CreateContextAsync();

        var otherRoute =
            await CreateRouteAsync(
                tenant2.TenantId);

        var request = new CreateClientRequest
        {
            FirstName = "Cliente",
            LastName = "Otro Tenant",
            Phone =
                $"809{Random.Shared.Next(1000000, 9999999)}",
            CollectionRouteId = otherRoute.Id
        };

        var response =
            await tenant1.Client.PostAsJsonAsync(
                "/api/clients",
                request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task Create_WithInactiveCollectionRoute_ReturnsBadRequest()
    {
        var context = await CreateContextAsync();

        var route =
            await CreateRouteAsync(
                context.TenantId);

        await DeactivateRouteAsync(
            route.Id);

        var request = new CreateClientRequest
        {
            FirstName = "Cliente",
            LastName = "Ruta Inactiva",
            Phone =
                $"809{Random.Shared.Next(1000000, 9999999)}",
            CollectionRouteId = route.Id
        };

        var response =
            await context.Client.PostAsJsonAsync(
                "/api/clients",
                request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task Update_WithValidCollectionRoute_ChangesCollectionRouteId()
    {
        var context = await CreateContextAsync();

        var firstRoute =
            await CreateRouteAsync(
                context.TenantId);

        var secondRoute =
            await CreateRouteAsync(
                context.TenantId);

        var createdClient =
            await CreateClientAsync(
                context.Client,
                firstRoute.Id);

        var updateRequest =
            CreateUpdateRequest(
                createdClient,
                secondRoute.Id);

        var updateResponse =
            await context.Client.PutAsJsonAsync(
                $"/api/clients/{createdClient.Id}",
                updateRequest);

        Assert.Equal(
            HttpStatusCode.OK,
            updateResponse.StatusCode);

        var updatedClient =
            await updateResponse.Content
                .ReadFromJsonAsync<ClientResponse>();

        Assert.NotNull(updatedClient);

        Assert.Equal(
            secondRoute.Id,
            updatedClient.CollectionRouteId);
    }

    [Fact]
    public async Task Update_WithNonExistingCollectionRoute_ReturnsBadRequest()
    {
        var context = await CreateContextAsync();

        var route =
            await CreateRouteAsync(
                context.TenantId);

        var createdClient =
            await CreateClientAsync(
                context.Client,
                route.Id);

        var updateRequest =
            CreateUpdateRequest(
                createdClient,
                Guid.NewGuid());

        var updateResponse =
            await context.Client.PutAsJsonAsync(
                $"/api/clients/{createdClient.Id}",
                updateRequest);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            updateResponse.StatusCode);
    }

    [Fact]
    public async Task Update_WithNullCollectionRoute_RemovesCollectionRoute()
    {
        var context = await CreateContextAsync();

        var route =
            await CreateRouteAsync(
                context.TenantId);

        var createdClient =
            await CreateClientAsync(
                context.Client,
                route.Id);

        Assert.Equal(
            route.Id,
            createdClient.CollectionRouteId);

        var updateRequest =
            CreateUpdateRequest(
                createdClient,
                null);

        var updateResponse =
            await context.Client.PutAsJsonAsync(
                $"/api/clients/{createdClient.Id}",
                updateRequest);

        Assert.Equal(
            HttpStatusCode.OK,
            updateResponse.StatusCode);

        var updatedClient =
            await updateResponse.Content
                .ReadFromJsonAsync<ClientResponse>();

        Assert.NotNull(updatedClient);
        Assert.Null(
            updatedClient.CollectionRouteId);
    }

    [Fact]
    public async Task GetAll_WithCollectionRoute_ReturnsOnlyClientsFromThatRoute()
    {
        var context = await CreateContextAsync();

        var route1 =
            await CreateRouteAsync(
                context.TenantId);

        var route2 =
            await CreateRouteAsync(
                context.TenantId);

        var clientRoute1 =
            await CreateClientAsync(
                context.Client,
                route1.Id);

        await CreateClientAsync(
            context.Client,
            route2.Id);

        var response =
            await context.Client.GetAsync(
                $"/api/clients" +
                $"?collectionRouteId={route1.Id}");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var clients =
            await response.Content
                .ReadFromJsonAsync<
                    List<ClientResponse>>();

        Assert.NotNull(clients);

        Assert.Contains(
            clients,
            x => x.Id == clientRoute1.Id);

        Assert.DoesNotContain(
            clients,
            x =>
                x.CollectionRouteId ==
                route2.Id);

        Assert.All(
            clients,
            x => Assert.Equal(
                route1.Id,
                x.CollectionRouteId));
    }

    [Fact]
    public async Task GetAll_WithoutCollectionRoute_ReturnsClientsFromDifferentRoutes()
    {
        var context = await CreateContextAsync();

        var route1 =
            await CreateRouteAsync(
                context.TenantId);

        var route2 =
            await CreateRouteAsync(
                context.TenantId);

        var clientRoute1 =
            await CreateClientAsync(
                context.Client,
                route1.Id);

        var clientRoute2 =
            await CreateClientAsync(
                context.Client,
                route2.Id);

        var response =
            await context.Client.GetAsync(
                "/api/clients");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var clients =
            await response.Content
                .ReadFromJsonAsync<
                    List<ClientResponse>>();

        Assert.NotNull(clients);

        Assert.Contains(
            clients,
            x => x.Id == clientRoute1.Id);

        Assert.Contains(
            clients,
            x => x.Id == clientRoute2.Id);

        Assert.All(
            clients,
            x => Assert.Equal(
                context.TenantId,
                x.TenantId));
    }

    [Fact]
    public async Task GetAll_WithNonExistingCollectionRoute_ReturnsEmptyList()
    {
        var context = await CreateContextAsync();

        var nonExistingRouteId =
            Guid.NewGuid();

        var response =
            await context.Client.GetAsync(
                $"/api/clients" +
                $"?collectionRouteId={nonExistingRouteId}");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var clients =
            await response.Content
                .ReadFromJsonAsync<
                    List<ClientResponse>>();

        Assert.NotNull(clients);
        Assert.Empty(clients);
    }

    [Fact]
    public async Task GetAll_WithCollectionRouteFromDifferentTenant_DoesNotExposeClients()
    {
        var tenant1 = await CreateContextAsync();
        var tenant2 = await CreateContextAsync();

        var otherRoute =
            await CreateRouteAsync(
                tenant2.TenantId);

        await CreateClientAsync(
            tenant2.Client,
            otherRoute.Id);

        var response =
            await tenant1.Client.GetAsync(
                $"/api/clients" +
                $"?collectionRouteId={otherRoute.Id}");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var clients =
            await response.Content
                .ReadFromJsonAsync<
                    List<ClientResponse>>();

        Assert.NotNull(clients);
        Assert.Empty(clients);
    }

    [Fact]
    public async Task GetById_ClientFromDifferentTenant_ReturnsNotFound()
    {
        var tenant1 = await CreateContextAsync();
        var tenant2 = await CreateContextAsync();

        var client =
            await CreateClientAsync(
                tenant1.Client);

        var response =
            await tenant2.Client.GetAsync(
                $"/api/clients/{client.Id}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task Update_ClientFromDifferentTenant_ReturnsNotFound()
    {
        var tenant1 = await CreateContextAsync();
        var tenant2 = await CreateContextAsync();

        var client =
            await CreateClientAsync(
                tenant1.Client);

        var request =
            CreateUpdateRequest(
                client,
                client.CollectionRouteId);

        var response =
            await tenant2.Client.PutAsJsonAsync(
                $"/api/clients/{client.Id}",
                request);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task GetAll_WithoutIncludeInactive_ExcludesInactiveClients()
    {
        var context =
            await CreateContextAsync();

        var activeClient =
            await CreateClientAsync(
                context.Client);

        var inactiveClient =
            await CreateClientAsync(
                context.Client);

        var deleteResponse =
            await context.Client.DeleteAsync(
                $"/api/clients/{inactiveClient.Id}");

        Assert.Equal(
            HttpStatusCode.NoContent,
            deleteResponse.StatusCode);

        var response =
            await context.Client.GetAsync(
                "/api/clients");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var clients =
            await response.Content
                .ReadFromJsonAsync<
                    List<ClientResponse>>();

        Assert.NotNull(clients);

        Assert.Contains(
            clients!,
            x =>
                x.Id == activeClient.Id);

        Assert.DoesNotContain(
            clients!,
            x =>
                x.Id == inactiveClient.Id);

        Assert.All(
            clients!,
            x =>
                Assert.True(
                    x.IsActive));
    }

    [Fact]
    public async Task GetAll_WithIncludeInactive_ReturnsActiveAndInactiveClients()
    {
        var context =
            await CreateContextAsync();

        var activeClient =
            await CreateClientAsync(
                context.Client);

        var inactiveClient =
            await CreateClientAsync(
                context.Client);

        var deleteResponse =
            await context.Client.DeleteAsync(
                $"/api/clients/{inactiveClient.Id}");

        Assert.Equal(
            HttpStatusCode.NoContent,
            deleteResponse.StatusCode);

        var response =
            await context.Client.GetAsync(
                "/api/clients?includeInactive=true");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var clients =
            await response.Content
                .ReadFromJsonAsync<
                    List<ClientResponse>>();

        Assert.NotNull(clients);

        var activeResult =
            Assert.Single(
                clients!,
                x =>
                    x.Id == activeClient.Id);

        var inactiveResult =
            Assert.Single(
                clients!,
                x =>
                    x.Id == inactiveClient.Id);

        Assert.True(
            activeResult.IsActive);

        Assert.False(
            inactiveResult.IsActive);
    }

    [Fact]
    public async Task GetAll_WithIncludeInactive_DoesNotExposeOtherTenantClients()
    {
        var tenant1 =
            await CreateContextAsync();

        var tenant2 =
            await CreateContextAsync();

        var tenant1Client =
            await CreateClientAsync(
                tenant1.Client);

        var tenant2Client =
            await CreateClientAsync(
                tenant2.Client);

        var response =
            await tenant1.Client.GetAsync(
                "/api/clients?includeInactive=true");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var clients =
            await response.Content
                .ReadFromJsonAsync<
                    List<ClientResponse>>();

        Assert.NotNull(clients);

        Assert.Contains(
            clients!,
            x =>
                x.Id == tenant1Client.Id);

        Assert.DoesNotContain(
            clients!,
            x =>
                x.Id == tenant2Client.Id);

        Assert.All(
            clients!,
            x =>
                Assert.Equal(
                    tenant1.TenantId,
                    x.TenantId));
    }

    [Fact]
    public async Task GetAll_WithRouteAndIncludeInactive_PreservesRouteFilter()
    {
        var context =
            await CreateContextAsync();

        var route1 =
            await CreateRouteAsync(
                context.TenantId);

        var route2 =
            await CreateRouteAsync(
                context.TenantId);

        var activeRoute1 =
            await CreateClientAsync(
                context.Client,
                route1.Id);

        var inactiveRoute1 =
            await CreateClientAsync(
                context.Client,
                route1.Id);

        var route2Client =
            await CreateClientAsync(
                context.Client,
                route2.Id);

        var deleteResponse =
            await context.Client.DeleteAsync(
                $"/api/clients/{inactiveRoute1.Id}");

        Assert.Equal(
            HttpStatusCode.NoContent,
            deleteResponse.StatusCode);

        var response =
            await context.Client.GetAsync(
                $"/api/clients" +
                $"?collectionRouteId={route1.Id}" +
                "&includeInactive=true");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var clients =
            await response.Content
                .ReadFromJsonAsync<
                    List<ClientResponse>>();

        Assert.NotNull(clients);

        Assert.Contains(
            clients!,
            x =>
                x.Id == activeRoute1.Id);

        Assert.Contains(
            clients!,
            x =>
                x.Id == inactiveRoute1.Id);

        Assert.DoesNotContain(
            clients!,
            x =>
                x.Id == route2Client.Id);

        Assert.All(
            clients!,
            x =>
                Assert.Equal(
                    route1.Id,
                    x.CollectionRouteId));
    }

    // ============================================================
    // HELPERS
    // ============================================================

    private async Task<TestTenantContext>
        CreateContextAsync()
    {
        return await TestAuthenticationHelper
            .CreateAdministratorContextAsync(
                _factory);
    }

    private async Task<CollectionRoute>
        CreateRouteAsync(
            Guid tenantId)
    {
        using var scope =
            _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<SanesDbContext>();

        var route = new CollectionRoute
        {
            TenantId = tenantId,
            Name =
                $"Ruta Test {Guid.NewGuid():N}",
            Description =
                "Ruta para integration test",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        dbContext.CollectionRoutes.Add(route);

        await dbContext.SaveChangesAsync();

        return route;
    }

    private async Task DeactivateRouteAsync(
        Guid routeId)
    {
        using var scope =
            _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<SanesDbContext>();

        var route =
            await dbContext.CollectionRoutes
                .SingleAsync(
                    x => x.Id == routeId);

        route.IsActive = false;
        route.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();
    }

    private static async Task<ClientResponse>
        CreateClientAsync(
            HttpClient client,
            Guid? collectionRouteId = null)
    {
        var request = new CreateClientRequest
        {
            FirstName = "Cliente",
            LastName =
                $"Test {Guid.NewGuid():N}",
            Phone =
                $"809{Random.Shared.Next(1000000, 9999999)}",
            CollectionRouteId =
                collectionRouteId
        };

        var response =
            await client.PostAsJsonAsync(
                "/api/clients",
                request);

        response.EnsureSuccessStatusCode();

        var createdClient =
            await response.Content
                .ReadFromJsonAsync<ClientResponse>();

        Assert.NotNull(createdClient);

        return createdClient;
    }

    private static UpdateClientRequest
        CreateUpdateRequest(
            ClientResponse client,
            Guid? collectionRouteId)
    {
        return new UpdateClientRequest
        {
            FirstName = client.FirstName,
            LastName = client.LastName,
            Phone = client.Phone,
            SecondaryPhone =
                client.SecondaryPhone,
            IdentificationType =
                client.IdentificationType,
            Identification =
                client.Identification,
            SocialNumber =
                client.SocialNumber,
            Address = client.Address,
            Latitude = client.Latitude,
            Longitude = client.Longitude,
            Notes = client.Notes,
            CollectionRouteId =
                collectionRouteId
        };
    }
}