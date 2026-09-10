using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Sanes.Application.Clients.DTOs;
using Sanes.Domain.Entities;
using Sanes.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Sanes.Api.IntegrationTests.Clients;

public class ClientsTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private static readonly Guid TenantId =
        Guid.Parse("8d3fa46b-1553-413d-a30f-60638832a130");

    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public ClientsTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Create_WithValidCollectionRoute_SavesCollectionRouteId()
    {
        await EnsureTestTenantAsync();

        var route = await CreateRouteAsync();

        var request = new CreateClientRequest
        {
            TenantId = TenantId,
            FirstName = "Cliente",
            LastName = "Ruta",
            Phone = $"809{Random.Shared.Next(1000000, 9999999)}",
            CollectionRouteId = route.Id
        };

        var response = await _client.PostAsJsonAsync(
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
            route.Id,
            client.CollectionRouteId);
    }

    [Fact]
    public async Task Create_WithNonExistingCollectionRoute_ReturnsBadRequest()
    {
        await EnsureTestTenantAsync();

        var request = new CreateClientRequest
        {
            TenantId = TenantId,
            FirstName = "Cliente",
            LastName = "Ruta Inexistente",
            Phone = $"809{Random.Shared.Next(1000000, 9999999)}",
            CollectionRouteId = Guid.NewGuid()
        };

        var response = await _client.PostAsJsonAsync(
            "/api/clients",
            request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task Create_WithCollectionRouteFromDifferentTenant_ReturnsBadRequest()
    {
        await EnsureTestTenantAsync();

        var otherTenantId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext =
                scope.ServiceProvider
                    .GetRequiredService<SanesDbContext>();

            dbContext.Tenants.Add(
                new Tenant
                {
                    Id = otherTenantId,
                    Name = "Other Tenant",
                    LegalName = "Other Tenant SRL",
                    Phone = "8095551111",
                    Email = $"other-{Guid.NewGuid():N}@sanes.local",
                    CurrencyCode = "DOP",
                    CurrencySymbol = "RD$",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });

            var otherRoute = new CollectionRoute
            {
                TenantId = otherTenantId,
                Name = $"Ruta Otro Tenant {Guid.NewGuid():N}",
                Description = "Ruta perteneciente a otro tenant",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            dbContext.CollectionRoutes.Add(otherRoute);

            await dbContext.SaveChangesAsync();

            var request = new CreateClientRequest
            {
                TenantId = TenantId,
                FirstName = "Cliente",
                LastName = "Otro Tenant",
                Phone = $"809{Random.Shared.Next(1000000, 9999999)}",
                CollectionRouteId = otherRoute.Id
            };

            var response = await _client.PostAsJsonAsync(
                "/api/clients",
                request);

            Assert.Equal(
                HttpStatusCode.BadRequest,
                response.StatusCode);
        }
    }

    [Fact]
    public async Task Create_WithInactiveCollectionRoute_ReturnsBadRequest()
    {
        await EnsureTestTenantAsync();

        var route = await CreateRouteAsync();

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext =
                scope.ServiceProvider
                    .GetRequiredService<SanesDbContext>();

            var routeToDeactivate =
                await dbContext.CollectionRoutes
                    .SingleAsync(x => x.Id == route.Id);

            routeToDeactivate.IsActive = false;
            routeToDeactivate.UpdatedAt = DateTime.UtcNow;

            await dbContext.SaveChangesAsync();
        }

        var request = new CreateClientRequest
        {
            TenantId = TenantId,
            FirstName = "Cliente",
            LastName = "Ruta Inactiva",
            Phone = $"809{Random.Shared.Next(1000000, 9999999)}",
            CollectionRouteId = route.Id
        };

        var response = await _client.PostAsJsonAsync(
            "/api/clients",
            request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task Update_WithValidCollectionRoute_ChangesCollectionRouteId()
    {
        await EnsureTestTenantAsync();

        var firstRoute = await CreateRouteAsync();
        var secondRoute = await CreateRouteAsync();

        var createRequest = new CreateClientRequest
        {
            TenantId = TenantId,
            FirstName = "Cliente",
            LastName = "Actualizar Ruta",
            Phone = $"809{Random.Shared.Next(1000000, 9999999)}",
            CollectionRouteId = firstRoute.Id
        };

        var createResponse = await _client.PostAsJsonAsync(
            "/api/clients",
            createRequest);

        createResponse.EnsureSuccessStatusCode();

        var createdClient =
            await createResponse.Content
                .ReadFromJsonAsync<ClientResponse>();

        Assert.NotNull(createdClient);

        var updateRequest = new UpdateClientRequest
        {
            FirstName = createdClient.FirstName,
            LastName = createdClient.LastName,
            Phone = createdClient.Phone,
            SecondaryPhone = createdClient.SecondaryPhone,
            IdentificationType = createdClient.IdentificationType,
            Identification = createdClient.Identification,
            SocialNumber = createdClient.SocialNumber,
            Address = createdClient.Address,
            Latitude = createdClient.Latitude,
            Longitude = createdClient.Longitude,
            Notes = createdClient.Notes,
            CollectionRouteId = secondRoute.Id
        };

        var updateResponse = await _client.PutAsJsonAsync(
            $"/api/clients/{createdClient.Id}?tenantId={TenantId}",
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
        await EnsureTestTenantAsync();

        var route = await CreateRouteAsync();

        var createRequest = new CreateClientRequest
        {
            TenantId = TenantId,
            FirstName = "Cliente",
            LastName = "Update Ruta Inexistente",
            Phone = $"809{Random.Shared.Next(1000000, 9999999)}",
            CollectionRouteId = route.Id
        };

        var createResponse = await _client.PostAsJsonAsync(
            "/api/clients",
            createRequest);

        createResponse.EnsureSuccessStatusCode();

        var createdClient =
            await createResponse.Content
                .ReadFromJsonAsync<ClientResponse>();

        Assert.NotNull(createdClient);

        var updateRequest = new UpdateClientRequest
        {
            FirstName = createdClient.FirstName,
            LastName = createdClient.LastName,
            Phone = createdClient.Phone,
            SecondaryPhone = createdClient.SecondaryPhone,
            IdentificationType = createdClient.IdentificationType,
            Identification = createdClient.Identification,
            SocialNumber = createdClient.SocialNumber,
            Address = createdClient.Address,
            Latitude = createdClient.Latitude,
            Longitude = createdClient.Longitude,
            Notes = createdClient.Notes,
            CollectionRouteId = Guid.NewGuid()
        };

        var updateResponse = await _client.PutAsJsonAsync(
            $"/api/clients/{createdClient.Id}?tenantId={TenantId}",
            updateRequest);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            updateResponse.StatusCode);
    }

    [Fact]
    public async Task Update_WithNullCollectionRoute_RemovesCollectionRoute()
    {
        await EnsureTestTenantAsync();

        var route = await CreateRouteAsync();

        var createRequest = new CreateClientRequest
        {
            TenantId = TenantId,
            FirstName = "Cliente",
            LastName = "Sin Ruta",
            Phone = $"809{Random.Shared.Next(1000000, 9999999)}",
            CollectionRouteId = route.Id
        };

        var createResponse = await _client.PostAsJsonAsync(
            "/api/clients",
            createRequest);

        createResponse.EnsureSuccessStatusCode();

        var createdClient =
            await createResponse.Content
                .ReadFromJsonAsync<ClientResponse>();

        Assert.NotNull(createdClient);
        Assert.Equal(
            route.Id,
            createdClient.CollectionRouteId);

        var updateRequest = new UpdateClientRequest
        {
            FirstName = createdClient.FirstName,
            LastName = createdClient.LastName,
            Phone = createdClient.Phone,
            SecondaryPhone = createdClient.SecondaryPhone,
            IdentificationType = createdClient.IdentificationType,
            Identification = createdClient.Identification,
            SocialNumber = createdClient.SocialNumber,
            Address = createdClient.Address,
            Latitude = createdClient.Latitude,
            Longitude = createdClient.Longitude,
            Notes = createdClient.Notes,
            CollectionRouteId = null
        };

        var updateResponse = await _client.PutAsJsonAsync(
            $"/api/clients/{createdClient.Id}?tenantId={TenantId}",
            updateRequest);

        Assert.Equal(
            HttpStatusCode.OK,
            updateResponse.StatusCode);

        var updatedClient =
            await updateResponse.Content
                .ReadFromJsonAsync<ClientResponse>();

        Assert.NotNull(updatedClient);
        Assert.Null(updatedClient.CollectionRouteId);
    }

    [Fact]
    public async Task GetAll_WithCollectionRoute_ReturnsOnlyClientsFromThatRoute()
    {
        await EnsureTestTenantAsync();

        var route1 = await CreateRouteAsync();
        var route2 = await CreateRouteAsync();

        var clientRoute1 = await CreateClientAsync(route1.Id);
        await CreateClientAsync(route2.Id);

        var response = await _client.GetAsync(
            $"/api/clients?tenantId={TenantId}&collectionRouteId={route1.Id}");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var clients =
            await response.Content
                .ReadFromJsonAsync<List<ClientResponse>>();

        Assert.NotNull(clients);

        Assert.Contains(
            clients,
            x => x.Id == clientRoute1.Id);

        Assert.DoesNotContain(
            clients,
            x => x.CollectionRouteId == route2.Id);

        Assert.All(
            clients,
            x => Assert.Equal(
                route1.Id,
                x.CollectionRouteId));
    }

    [Fact]
    public async Task GetAll_WithoutCollectionRoute_ReturnsClientsFromDifferentRoutes()
    {
        await EnsureTestTenantAsync();

        var route1 = await CreateRouteAsync();
        var route2 = await CreateRouteAsync();

        var clientRoute1 = await CreateClientAsync(route1.Id);
        var clientRoute2 = await CreateClientAsync(route2.Id);

        var response = await _client.GetAsync(
            $"/api/clients?tenantId={TenantId}");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var clients =
            await response.Content
                .ReadFromJsonAsync<List<ClientResponse>>();

        Assert.NotNull(clients);

        Assert.Contains(
            clients,
            x => x.Id == clientRoute1.Id);

        Assert.Contains(
            clients,
            x => x.Id == clientRoute2.Id);
    }

    [Fact]
    public async Task GetAll_WithNonExistingCollectionRoute_ReturnsEmptyList()
    {
        await EnsureTestTenantAsync();

        var nonExistingRouteId = Guid.NewGuid();

        var response = await _client.GetAsync(
            $"/api/clients?tenantId={TenantId}&collectionRouteId={nonExistingRouteId}");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var clients =
            await response.Content
                .ReadFromJsonAsync<List<ClientResponse>>();

        Assert.NotNull(clients);
        Assert.Empty(clients);
    }

    [Fact]
    public async Task GetAll_WithCollectionRouteFromDifferentTenant_DoesNotExposeClients()
    {
        await EnsureTestTenantAsync();

        var otherTenantId = Guid.NewGuid();
        Guid otherRouteId;

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext =
                scope.ServiceProvider
                    .GetRequiredService<SanesDbContext>();

            var otherTenant = new Tenant
            {
                Id = otherTenantId,
                Name = $"Other Tenant {Guid.NewGuid():N}",
                LegalName = "Other Tenant SRL",
                Phone = "8095552222",
                Email = $"other-{Guid.NewGuid():N}@sanes.local",
                CurrencyCode = "DOP",
                CurrencySymbol = "RD$",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            dbContext.Tenants.Add(otherTenant);

            var otherRoute = new CollectionRoute
            {
                TenantId = otherTenantId,
                Name = $"Ruta Otro Tenant {Guid.NewGuid():N}",
                Description = "Ruta de otro tenant",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            dbContext.CollectionRoutes.Add(otherRoute);

            await dbContext.SaveChangesAsync();

            otherRouteId = otherRoute.Id;
        }

        var response = await _client.GetAsync(
            $"/api/clients?tenantId={TenantId}&collectionRouteId={otherRouteId}");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var clients =
            await response.Content
                .ReadFromJsonAsync<List<ClientResponse>>();

        Assert.NotNull(clients);
        Assert.Empty(clients);
    }

    private async Task<CollectionRoute> CreateRouteAsync()
    {
        using var scope =
            _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<SanesDbContext>();

        var route = new CollectionRoute
        {
            TenantId = TenantId,
            Name = $"Ruta Test {Guid.NewGuid():N}",
            Description = "Ruta para integration test",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        dbContext.CollectionRoutes.Add(route);

        await dbContext.SaveChangesAsync();

        return route;
    }

    private async Task EnsureTestTenantAsync()
    {
        using var scope =
            _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<SanesDbContext>();

        var exists =
            await dbContext.Tenants.AnyAsync(
                x => x.Id == TenantId);

        if (exists)
        {
            return;
        }

        dbContext.Tenants.Add(
            new Tenant
            {
                Id = TenantId,
                Name = "Sanes Integration Tests",
                LegalName = "Sanes Integration Tests SRL",
                Phone = "8095550000",
                Email = "integration-tests@sanes.local",
                CurrencyCode = "DOP",
                CurrencySymbol = "RD$",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

        await dbContext.SaveChangesAsync();
    }

    private async Task<ClientResponse> CreateClientAsync(
        Guid? collectionRouteId = null)
    {
        await EnsureTestTenantAsync();

        var request = new CreateClientRequest
        {
            TenantId = TenantId,
            FirstName = "Cliente",
            LastName = $"Test {Guid.NewGuid():N}",
            Phone = $"809{Random.Shared.Next(1000000, 9999999)}",
            CollectionRouteId = collectionRouteId
        };

        var response = await _client.PostAsJsonAsync(
            "/api/clients",
            request);

        response.EnsureSuccessStatusCode();

        var client =
            await response.Content
                .ReadFromJsonAsync<ClientResponse>();

        Assert.NotNull(client);

        return client;
    }
}