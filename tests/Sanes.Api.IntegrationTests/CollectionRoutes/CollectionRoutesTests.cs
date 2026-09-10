using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Sanes.Application.CollectionRoutes.DTOs;
using Sanes.Domain.Entities;
using Sanes.Infrastructure.Persistence;

namespace Sanes.Api.IntegrationTests.CollectionRoutes;

public class CollectionRoutesTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    private static readonly Guid TenantId =
        Guid.Parse("8d3fa46b-1553-413d-a30f-60638832a130");

    public CollectionRoutesTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Create_WithValidData_ReturnsCreated()
    {
        await EnsureTestTenantAsync();

        var request = new CreateCollectionRouteRequest
        {
            TenantId = TenantId,
            Name = $"Ruta Test {Guid.NewGuid():N}",
            Description = "Ruta creada mediante integration test"
        };

        var response = await _client.PostAsJsonAsync(
            "/api/collection-routes",
            request);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var route =
            await response.Content
                .ReadFromJsonAsync<CollectionRouteResponse>();

        Assert.NotNull(route);

        Assert.Equal(TenantId, route.TenantId);
        Assert.Equal(request.Name, route.Name);
        Assert.Equal(request.Description, route.Description);
        Assert.True(route.IsActive);
        Assert.NotEqual(Guid.Empty, route.Id);
    }

    private async Task EnsureTestTenantAsync()
    {
        using var scope = _factory.Services.CreateScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<SanesDbContext>();

        var tenant = await dbContext.Tenants
            .FindAsync(TenantId);

        if (tenant is not null)
        {
            return;
        }

        tenant = new Tenant
        {
            Id = TenantId,
            Name = "Sanes Integration Tests",
            LegalName = "Sanes Integration Tests SRL",
            Phone = "8095550000",
            Email = "integration-tests@sanes.local",
            CurrencyCode = "DOP",
            CurrencySymbol = "RD$",
            IsActive = true
        };

        dbContext.Tenants.Add(tenant);

        await dbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task GetById_WithValidTenant_ReturnsRoute()
    {
        var routeName = $"Ruta GET Test {Guid.NewGuid():N}";
        var description = "Ruta para probar GET por ID";

        var createdRoute = await CreateRouteAsync(
            routeName,
            description);

        var response = await _client.GetAsync(
            $"/api/collection-routes/{createdRoute.Id}?tenantId={TenantId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var route =
            await response.Content
                .ReadFromJsonAsync<CollectionRouteResponse>();

        Assert.NotNull(route);

        Assert.Equal(createdRoute.Id, route.Id);
        Assert.Equal(TenantId, route.TenantId);
        Assert.Equal(routeName, route.Name);
        Assert.Equal(description, route.Description);
        Assert.True(route.IsActive);
    }

    [Fact]
    public async Task GetById_WithDifferentTenant_ReturnsNotFound()
    {
        var createdRoute = await CreateRouteAsync(
            $"Ruta Isolation Test {Guid.NewGuid():N}",
            "Ruta para validar aislamiento por tenant");

        var otherTenantId = Guid.NewGuid();

        var response = await _client.GetAsync(
            $"/api/collection-routes/{createdRoute.Id}?tenantId={otherTenantId}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }
    [Fact]
    public async Task Update_WithValidData_ReturnsUpdatedRoute()
    {
        var createdRoute = await CreateRouteAsync(
            $"Ruta PUT Test {Guid.NewGuid():N}",
            "Descripción original");

        var updateRequest = new UpdateCollectionRouteRequest
        {
            Name = $"Ruta Actualizada {Guid.NewGuid():N}",
            Description = "Descripción actualizada mediante integration test"
        };

        var response = await _client.PutAsJsonAsync(
            $"/api/collection-routes/{createdRoute.Id}?tenantId={TenantId}",
            updateRequest);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updatedRoute =
            await response.Content
                .ReadFromJsonAsync<CollectionRouteResponse>();

        Assert.NotNull(updatedRoute);

        Assert.Equal(createdRoute.Id, updatedRoute.Id);
        Assert.Equal(TenantId, updatedRoute.TenantId);
        Assert.Equal(updateRequest.Name, updatedRoute.Name);
        Assert.Equal(updateRequest.Description, updatedRoute.Description);
        Assert.True(updatedRoute.IsActive);
    }

    [Fact]
    public async Task Delete_WithValidTenant_HidesRouteFromGet()
    {
        var createdRoute = await CreateRouteAsync(
            $"Ruta DELETE Test {Guid.NewGuid():N}",
            "Ruta para probar soft delete");

        var deleteResponse = await _client.DeleteAsync(
            $"/api/collection-routes/{createdRoute.Id}?tenantId={TenantId}");

        Assert.Equal(
            HttpStatusCode.NoContent,
            deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync(
            $"/api/collection-routes/{createdRoute.Id}?tenantId={TenantId}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            getResponse.StatusCode);
    }

    [Fact]
    public async Task Reactivate_AfterDelete_ReturnsActiveRoute()
    {
        var createdRoute = await CreateRouteAsync(
            $"Ruta Reactivate Test {Guid.NewGuid():N}",
            "Ruta para probar reactivación");

        var deleteResponse = await _client.DeleteAsync(
            $"/api/collection-routes/{createdRoute.Id}?tenantId={TenantId}");

        Assert.Equal(
            HttpStatusCode.NoContent,
            deleteResponse.StatusCode);

        var reactivateResponse = await _client.PatchAsync(
            $"/api/collection-routes/{createdRoute.Id}/reactivate?tenantId={TenantId}",
            null);

        Assert.Equal(
            HttpStatusCode.OK,
            reactivateResponse.StatusCode);

        var reactivatedRoute =
            await reactivateResponse.Content
                .ReadFromJsonAsync<CollectionRouteResponse>();

        Assert.NotNull(reactivatedRoute);

        Assert.Equal(createdRoute.Id, reactivatedRoute.Id);
        Assert.True(reactivatedRoute.IsActive);

        var getResponse = await _client.GetAsync(
            $"/api/collection-routes/{createdRoute.Id}?tenantId={TenantId}");

        Assert.Equal(
            HttpStatusCode.OK,
            getResponse.StatusCode);
    }

    [Fact]
    public async Task Create_WithEmptyName_ReturnsBadRequest()
    {
        await EnsureTestTenantAsync();

        var response = await PostRouteAsync(
            TenantId,
            "   ",
            "Nombre inválido");

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task Create_WithNonExistingTenant_ReturnsBadRequest()
    {
        var response = await PostRouteAsync(
            Guid.NewGuid(),
            $"Ruta Invalid Tenant {Guid.NewGuid():N}",
            "Ruta con tenant inexistente");

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }


    [Fact]
    public async Task Update_WithDifferentTenant_ReturnsNotFound()
    {
        var createdRoute = await CreateRouteAsync(
            $"Ruta PUT Isolation {Guid.NewGuid():N}",
            "Ruta para validar aislamiento en PUT");

        var updateRequest = new UpdateCollectionRouteRequest
        {
            Name = $"Ruta Modificada {Guid.NewGuid():N}",
            Description = "Intento de modificación desde otro tenant"
        };

        var otherTenantId = Guid.NewGuid();

        var response = await _client.PutAsJsonAsync(
            $"/api/collection-routes/{createdRoute.Id}?tenantId={otherTenantId}",
            updateRequest);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task GetAll_WithValidTenant_ReturnsActiveRoutes()
    {
        var routeName1 = $"Ruta List 1 {Guid.NewGuid():N}";
        var routeName2 = $"Ruta List 2 {Guid.NewGuid():N}";

        await CreateRouteAsync(
            routeName1,
            "Primera ruta para probar listado");

        await CreateRouteAsync(
            routeName2,
            "Segunda ruta para probar listado");

        var response = await _client.GetAsync(
            $"/api/collection-routes?tenantId={TenantId}");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var routes =
            await response.Content
                .ReadFromJsonAsync<List<CollectionRouteResponse>>();

        Assert.NotNull(routes);

        Assert.Contains(
            routes,
            x => x.Name == routeName1 &&
                x.TenantId == TenantId &&
                x.IsActive);

        Assert.Contains(
            routes,
            x => x.Name == routeName2 &&
                x.TenantId == TenantId &&
                x.IsActive);
    }

[Fact]
public async Task GetAll_AfterDelete_DoesNotReturnDeletedRoute()
{
    var createdRoute = await CreateRouteAsync(
        $"Ruta Deleted List Test {Guid.NewGuid():N}",
        "Ruta que no debe aparecer después del delete");

    var deleteResponse = await _client.DeleteAsync(
        $"/api/collection-routes/{createdRoute.Id}?tenantId={TenantId}");

    Assert.Equal(
        HttpStatusCode.NoContent,
        deleteResponse.StatusCode);

    var listResponse = await _client.GetAsync(
        $"/api/collection-routes?tenantId={TenantId}");

    Assert.Equal(
        HttpStatusCode.OK,
        listResponse.StatusCode);

    var routes =
        await listResponse.Content
            .ReadFromJsonAsync<List<CollectionRouteResponse>>();

    Assert.NotNull(routes);

    Assert.DoesNotContain(
        routes,
        x => x.Id == createdRoute.Id);
}

    private async Task<CollectionRouteResponse> CreateRouteAsync(
        string? name = null,
        string? description = null)
    {
        await EnsureTestTenantAsync();

        var routeName =
            name ?? $"Ruta Test {Guid.NewGuid():N}";

        var routeDescription =
            description ?? "Ruta creada mediante integration test";

        var response = await PostRouteAsync(
            TenantId,
            routeName,
            routeDescription);

        response.EnsureSuccessStatusCode();

        var route =
            await response.Content
                .ReadFromJsonAsync<CollectionRouteResponse>();

        Assert.NotNull(route);

        return route;
    }

    private async Task<HttpResponseMessage> PostRouteAsync(
        Guid tenantId,
        string name,
        string? description = null)
    {
        var request = new CreateCollectionRouteRequest
        {
            TenantId = tenantId,
            Name = name,
            Description = description
        };

        return await _client.PostAsJsonAsync(
            "/api/collection-routes",
            request);
    }
}