using System.Net;
using System.Net.Http.Json;
using Sanes.Api.IntegrationTests.Helpers;
using Sanes.Application.CollectionRoutes.DTOs;

namespace Sanes.Api.IntegrationTests.CollectionRoutes;

public class CollectionRoutesTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public CollectionRoutesTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Create_WithValidData_ReturnsCreated()
    {
        var context = await CreateContextAsync();

        var request =
            new CreateCollectionRouteRequest
            {
                Name =
                    $"Ruta Test {Guid.NewGuid():N}",
                Description =
                    "Ruta creada mediante integration test"
            };

        var response =
            await context.Client.PostAsJsonAsync(
                "/api/collection-routes",
                request);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var route =
            await response.Content
                .ReadFromJsonAsync<
                    CollectionRouteResponse>();

        Assert.NotNull(route);

        Assert.Equal(
            context.TenantId,
            route.TenantId);

        Assert.Equal(
            request.Name,
            route.Name);

        Assert.Equal(
            request.Description,
            route.Description);

        Assert.True(route.IsActive);
        Assert.NotEqual(Guid.Empty, route.Id);
    }

    [Fact]
    public async Task GetById_WithValidTenant_ReturnsRoute()
    {
        var context = await CreateContextAsync();

        var routeName =
            $"Ruta GET Test {Guid.NewGuid():N}";

        var description =
            "Ruta para probar GET por ID";

        var createdRoute =
            await CreateRouteAsync(
                context.Client,
                routeName,
                description);

        var response =
            await context.Client.GetAsync(
                $"/api/collection-routes/{createdRoute.Id}");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var route =
            await response.Content
                .ReadFromJsonAsync<
                    CollectionRouteResponse>();

        Assert.NotNull(route);

        Assert.Equal(
            createdRoute.Id,
            route.Id);

        Assert.Equal(
            context.TenantId,
            route.TenantId);

        Assert.Equal(
            routeName,
            route.Name);

        Assert.Equal(
            description,
            route.Description);

        Assert.True(route.IsActive);
    }

    [Fact]
    public async Task GetById_WithDifferentTenant_ReturnsNotFound()
    {
        var tenant1 = await CreateContextAsync();
        var tenant2 = await CreateContextAsync();

        var createdRoute =
            await CreateRouteAsync(
                tenant1.Client,
                $"Ruta Isolation Test {Guid.NewGuid():N}",
                "Ruta para validar aislamiento por tenant");

        var response =
            await tenant2.Client.GetAsync(
                $"/api/collection-routes/{createdRoute.Id}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task Update_WithValidData_ReturnsUpdatedRoute()
    {
        var context = await CreateContextAsync();

        var createdRoute =
            await CreateRouteAsync(
                context.Client,
                $"Ruta PUT Test {Guid.NewGuid():N}",
                "Descripción original");

        var updateRequest =
            new UpdateCollectionRouteRequest
            {
                Name =
                    $"Ruta Actualizada {Guid.NewGuid():N}",
                Description =
                    "Descripción actualizada mediante integration test"
            };

        var response =
            await context.Client.PutAsJsonAsync(
                $"/api/collection-routes/{createdRoute.Id}",
                updateRequest);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var updatedRoute =
            await response.Content
                .ReadFromJsonAsync<
                    CollectionRouteResponse>();

        Assert.NotNull(updatedRoute);

        Assert.Equal(
            createdRoute.Id,
            updatedRoute.Id);

        Assert.Equal(
            context.TenantId,
            updatedRoute.TenantId);

        Assert.Equal(
            updateRequest.Name,
            updatedRoute.Name);

        Assert.Equal(
            updateRequest.Description,
            updatedRoute.Description);

        Assert.True(updatedRoute.IsActive);
    }

    [Fact]
    public async Task Delete_WithValidTenant_HidesRouteFromGet()
    {
        var context = await CreateContextAsync();

        var createdRoute =
            await CreateRouteAsync(
                context.Client,
                $"Ruta DELETE Test {Guid.NewGuid():N}",
                "Ruta para probar soft delete");

        var deleteResponse =
            await context.Client.DeleteAsync(
                $"/api/collection-routes/{createdRoute.Id}");

        Assert.Equal(
            HttpStatusCode.NoContent,
            deleteResponse.StatusCode);

        var getResponse =
            await context.Client.GetAsync(
                $"/api/collection-routes/{createdRoute.Id}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            getResponse.StatusCode);
    }

    [Fact]
    public async Task Reactivate_AfterDelete_ReturnsActiveRoute()
    {
        var context = await CreateContextAsync();

        var createdRoute =
            await CreateRouteAsync(
                context.Client,
                $"Ruta Reactivate Test {Guid.NewGuid():N}",
                "Ruta para probar reactivación");

        var deleteResponse =
            await context.Client.DeleteAsync(
                $"/api/collection-routes/{createdRoute.Id}");

        Assert.Equal(
            HttpStatusCode.NoContent,
            deleteResponse.StatusCode);

        var reactivateResponse =
            await context.Client.PatchAsync(
                $"/api/collection-routes/{createdRoute.Id}/reactivate",
                null);

        Assert.Equal(
            HttpStatusCode.OK,
            reactivateResponse.StatusCode);

        var reactivatedRoute =
            await reactivateResponse.Content
                .ReadFromJsonAsync<
                    CollectionRouteResponse>();

        Assert.NotNull(reactivatedRoute);

        Assert.Equal(
            createdRoute.Id,
            reactivatedRoute.Id);

        Assert.True(
            reactivatedRoute.IsActive);

        var getResponse =
            await context.Client.GetAsync(
                $"/api/collection-routes/{createdRoute.Id}");

        Assert.Equal(
            HttpStatusCode.OK,
            getResponse.StatusCode);
    }

    [Fact]
    public async Task Create_WithEmptyName_ReturnsBadRequest()
    {
        var context = await CreateContextAsync();

        var response =
            await PostRouteAsync(
                context.Client,
                "   ",
                "Nombre inválido");

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task Update_WithDifferentTenant_ReturnsNotFound()
    {
        var tenant1 = await CreateContextAsync();
        var tenant2 = await CreateContextAsync();

        var createdRoute =
            await CreateRouteAsync(
                tenant1.Client,
                $"Ruta PUT Isolation {Guid.NewGuid():N}",
                "Ruta para validar aislamiento en PUT");

        var updateRequest =
            new UpdateCollectionRouteRequest
            {
                Name =
                    $"Ruta Modificada {Guid.NewGuid():N}",
                Description =
                    "Intento de modificación desde otro tenant"
            };

        var response =
            await tenant2.Client.PutAsJsonAsync(
                $"/api/collection-routes/{createdRoute.Id}",
                updateRequest);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task GetAll_WithValidTenant_ReturnsActiveRoutes()
    {
        var context = await CreateContextAsync();

        var routeName1 =
            $"Ruta List 1 {Guid.NewGuid():N}";

        var routeName2 =
            $"Ruta List 2 {Guid.NewGuid():N}";

        await CreateRouteAsync(
            context.Client,
            routeName1,
            "Primera ruta para probar listado");

        await CreateRouteAsync(
            context.Client,
            routeName2,
            "Segunda ruta para probar listado");

        var response =
            await context.Client.GetAsync(
                "/api/collection-routes");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var routes =
            await response.Content
                .ReadFromJsonAsync<
                    List<CollectionRouteResponse>>();

        Assert.NotNull(routes);

        Assert.Contains(
            routes,
            x =>
                x.Name == routeName1 &&
                x.TenantId == context.TenantId &&
                x.IsActive);

        Assert.Contains(
            routes,
            x =>
                x.Name == routeName2 &&
                x.TenantId == context.TenantId &&
                x.IsActive);

        Assert.All(
            routes,
            x => Assert.Equal(
                context.TenantId,
                x.TenantId));
    }

    [Fact]
    public async Task GetAll_AfterDelete_DoesNotReturnDeletedRoute()
    {
        var context = await CreateContextAsync();

        var createdRoute =
            await CreateRouteAsync(
                context.Client,
                $"Ruta Deleted List Test {Guid.NewGuid():N}",
                "Ruta que no debe aparecer después del delete");

        var deleteResponse =
            await context.Client.DeleteAsync(
                $"/api/collection-routes/{createdRoute.Id}");

        Assert.Equal(
            HttpStatusCode.NoContent,
            deleteResponse.StatusCode);

        var listResponse =
            await context.Client.GetAsync(
                "/api/collection-routes");

        Assert.Equal(
            HttpStatusCode.OK,
            listResponse.StatusCode);

        var routes =
            await listResponse.Content
                .ReadFromJsonAsync<
                    List<CollectionRouteResponse>>();

        Assert.NotNull(routes);

        Assert.DoesNotContain(
            routes,
            x => x.Id == createdRoute.Id);
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

    private static async Task<CollectionRouteResponse>
        CreateRouteAsync(
            HttpClient client,
            string? name = null,
            string? description = null)
    {
        var routeName =
            name ??
            $"Ruta Test {Guid.NewGuid():N}";

        var routeDescription =
            description ??
            "Ruta creada mediante integration test";

        var response =
            await PostRouteAsync(
                client,
                routeName,
                routeDescription);

        response.EnsureSuccessStatusCode();

        var route =
            await response.Content
                .ReadFromJsonAsync<
                    CollectionRouteResponse>();

        Assert.NotNull(route);

        return route;
    }

    private static async Task<HttpResponseMessage>
        PostRouteAsync(
            HttpClient client,
            string name,
            string? description = null)
    {
        var request =
            new CreateCollectionRouteRequest
            {
                Name = name,
                Description = description
            };

        return await client.PostAsJsonAsync(
            "/api/collection-routes",
            request);
    }
}