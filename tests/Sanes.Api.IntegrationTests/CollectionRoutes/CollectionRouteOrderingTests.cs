using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Sanes.Api.IntegrationTests.Helpers;

namespace Sanes.Api.IntegrationTests.CollectionRoutes;

public class CollectionRouteOrderingTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public CollectionRouteOrderingTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ============================================================
    // ORDER MODE
    // ============================================================

    [Fact]
    public async Task Create_WithManualOrderMode_ReturnsManual()
    {
        var context = await CreateContextAsync();

        var route = await CreateRouteAsync(
            context.Client,
            orderMode: 1);

        Assert.Equal(1, route.OrderMode);
    }

    [Fact]
    public async Task Create_WithAutomaticOrderMode_ReturnsAutomatic()
    {
        var context = await CreateContextAsync();

        var route = await CreateRouteAsync(
            context.Client,
            orderMode: 2);

        Assert.Equal(2, route.OrderMode);
    }

    [Fact]
    public async Task Create_WithInvalidOrderMode_ReturnsBadRequest()
    {
        var context = await CreateContextAsync();

        var response =
            await context.Client.PostAsJsonAsync(
                "/api/collection-routes",
                new
                {
                    name = Unique("Ruta invalida"),
                    description = "OrderMode invalido",
                    orderMode = 99
                });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    // ============================================================
    // MANUAL ORDERING
    // ============================================================

    [Fact]
    public async Task ReorderClients_ManualRoute_AssignsSequentialOrder()
    {
        var context = await CreateContextAsync();

        var route = await CreateRouteAsync(
            context.Client,
            1);

        var clientA = await CreateClientAsync(
            context.Client,
            route.Id,
            "Cliente A");

        var clientB = await CreateClientAsync(
            context.Client,
            route.Id,
            "Cliente B");

        var clientC = await CreateClientAsync(
            context.Client,
            route.Id,
            "Cliente C");

        var response = await ReorderAsync(
            context.Client,
            route.Id,
            clientC.Id,
            clientA.Id,
            clientB.Id);

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        var clients = await GetRouteClientsAsync(
            context.Client,
            route.Id);

        Assert.Equal(3, clients.Count);

        Assert.Equal(clientC.Id, clients[0].Id);
        Assert.Equal(1, clients[0].CollectionRouteOrder);

        Assert.Equal(clientA.Id, clients[1].Id);
        Assert.Equal(2, clients[1].CollectionRouteOrder);

        Assert.Equal(clientB.Id, clients[2].Id);
        Assert.Equal(3, clients[2].CollectionRouteOrder);
    }

    [Fact]
    public async Task ReorderClients_OmittedClient_SetsOrderToNull()
    {
        var context = await CreateContextAsync();

        var route = await CreateRouteAsync(
            context.Client,
            1);

        var clientA = await CreateClientAsync(
            context.Client,
            route.Id,
            "Cliente A");

        var clientB = await CreateClientAsync(
            context.Client,
            route.Id,
            "Cliente B");

        var clientC = await CreateClientAsync(
            context.Client,
            route.Id,
            "Cliente C");

        var firstOrder = await ReorderAsync(
            context.Client,
            route.Id,
            clientA.Id,
            clientB.Id,
            clientC.Id);

        Assert.Equal(
            HttpStatusCode.NoContent,
            firstOrder.StatusCode);

        var secondOrder = await ReorderAsync(
            context.Client,
            route.Id,
            clientB.Id,
            clientA.Id);

        Assert.Equal(
            HttpStatusCode.NoContent,
            secondOrder.StatusCode);

        var clients = await GetRouteClientsAsync(
            context.Client,
            route.Id);

        var omittedClient =
            clients.Single(x => x.Id == clientC.Id);

        Assert.Null(
            omittedClient.CollectionRouteOrder);
    }

    [Fact]
    public async Task ReorderClients_WithDuplicateIds_ReturnsBadRequest()
    {
        var context = await CreateContextAsync();

        var route = await CreateRouteAsync(
            context.Client,
            1);

        var client = await CreateClientAsync(
            context.Client,
            route.Id,
            "Cliente duplicado");

        var response = await ReorderAsync(
            context.Client,
            route.Id,
            client.Id,
            client.Id);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task ReorderClients_WithClientFromDifferentRoute_ReturnsBadRequest()
    {
        var context = await CreateContextAsync();

        var routeA = await CreateRouteAsync(
            context.Client,
            1);

        var routeB = await CreateRouteAsync(
            context.Client,
            1);

        var clientA = await CreateClientAsync(
            context.Client,
            routeA.Id,
            "Cliente Ruta A");

        var clientB = await CreateClientAsync(
            context.Client,
            routeB.Id,
            "Cliente Ruta B");

        var response = await ReorderAsync(
            context.Client,
            routeA.Id,
            clientA.Id,
            clientB.Id);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task ReorderClients_WithClientFromDifferentTenant_ReturnsBadRequest()
    {
        var tenantA = await CreateContextAsync();
        var tenantB = await CreateContextAsync();

        var routeA = await CreateRouteAsync(
            tenantA.Client,
            1);

        var routeB = await CreateRouteAsync(
            tenantB.Client,
            1);

        var clientA = await CreateClientAsync(
            tenantA.Client,
            routeA.Id,
            "Cliente Tenant A");

        var clientB = await CreateClientAsync(
            tenantB.Client,
            routeB.Id,
            "Cliente Tenant B");

        var response = await ReorderAsync(
            tenantA.Client,
            routeA.Id,
            clientA.Id,
            clientB.Id);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task ReorderClients_OnAutomaticRoute_ReturnsBadRequest()
    {
        var context = await CreateContextAsync();

        var route = await CreateRouteAsync(
            context.Client,
            2);

        var client = await CreateClientAsync(
            context.Client,
            route.Id,
            "Cliente Automatic");

        var response = await ReorderAsync(
            context.Client,
            route.Id,
            client.Id);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task ReorderClients_RouteFromDifferentTenant_ReturnsNotFound()
    {
        var tenantA = await CreateContextAsync();
        var tenantB = await CreateContextAsync();

        var route = await CreateRouteAsync(
            tenantA.Client,
            1);

        var response = await ReorderAsync(
            tenantB.Client,
            route.Id);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    // ============================================================
    // CLIENT LIST ORDER
    // ============================================================

    [Fact]
    public async Task GetClientsByRoute_ReturnsClientsInCollectionRouteOrder()
    {
        var context = await CreateContextAsync();

        var route = await CreateRouteAsync(
            context.Client,
            1);

        var clientA = await CreateClientAsync(
            context.Client,
            route.Id,
            "AAA");

        var clientB = await CreateClientAsync(
            context.Client,
            route.Id,
            "BBB");

        var clientC = await CreateClientAsync(
            context.Client,
            route.Id,
            "CCC");

        await ReorderAsync(
            context.Client,
            route.Id,
            clientC.Id,
            clientA.Id,
            clientB.Id);

        var clients = await GetRouteClientsAsync(
            context.Client,
            route.Id);

        Assert.Equal(
            new[]
            {
                clientC.Id,
                clientA.Id,
                clientB.Id
            },
            clients.Select(x => x.Id).ToArray());
    }

    [Fact]
    public async Task GetClientsByRoute_UnorderedClientsAppearLast()
    {
        var context = await CreateContextAsync();

        var route = await CreateRouteAsync(
            context.Client,
            1);

        var orderedA = await CreateClientAsync(
            context.Client,
            route.Id,
            "ZZZ Ordered A");

        var orderedB = await CreateClientAsync(
            context.Client,
            route.Id,
            "YYY Ordered B");

        var unordered = await CreateClientAsync(
            context.Client,
            route.Id,
            "AAA Unordered");

        await ReorderAsync(
            context.Client,
            route.Id,
            orderedB.Id,
            orderedA.Id);

        var clients = await GetRouteClientsAsync(
            context.Client,
            route.Id);

        Assert.Equal(orderedB.Id, clients[0].Id);
        Assert.Equal(orderedA.Id, clients[1].Id);
        Assert.Equal(unordered.Id, clients[2].Id);

        Assert.Equal(
            1,
            clients[0].CollectionRouteOrder);

        Assert.Equal(
            2,
            clients[1].CollectionRouteOrder);

        Assert.Null(
            clients[2].CollectionRouteOrder);
    }

    // ============================================================
    // CLIENT MOVEMENT BETWEEN ROUTES
    // ============================================================

    [Fact]
    public async Task UpdateClient_MovingToDifferentRoute_ClearsOrder()
    {
        var context = await CreateContextAsync();

        var routeA = await CreateRouteAsync(
            context.Client,
            1);

        var routeB = await CreateRouteAsync(
            context.Client,
            1);

        var client = await CreateClientAsync(
            context.Client,
            routeA.Id,
            "Cliente mover");

        await ReorderAsync(
            context.Client,
            routeA.Id,
            client.Id);

        var updateResponse = await UpdateClientAsync(
            context.Client,
            client,
            routeB.Id);

        Assert.Equal(
            HttpStatusCode.OK,
            updateResponse.StatusCode);

        var updatedClient =
            await GetClientAsync(
                context.Client,
                client.Id);

        Assert.Equal(
            routeB.Id,
            updatedClient.CollectionRouteId);

        Assert.Null(
            updatedClient.CollectionRouteOrder);
    }

    [Fact]
    public async Task UpdateClient_RemainingInSameRoute_PreservesOrder()
    {
        var context = await CreateContextAsync();

        var route = await CreateRouteAsync(
            context.Client,
            1);

        var client = await CreateClientAsync(
            context.Client,
            route.Id,
            "Cliente conserva");

        await ReorderAsync(
            context.Client,
            route.Id,
            client.Id);

        var updateResponse = await UpdateClientAsync(
            context.Client,
            client,
            route.Id,
            "Cliente actualizado");

        Assert.Equal(
            HttpStatusCode.OK,
            updateResponse.StatusCode);

        var updatedClient =
            await GetClientAsync(
                context.Client,
                client.Id);

        Assert.Equal(
            1,
            updatedClient.CollectionRouteOrder);
    }

    // ============================================================
    // AUTOMATIC OPTIMIZATION
    // ============================================================

    [Fact]
    public async Task Optimize_AutomaticRoute_AssignsOrderToClientsWithCoordinates()
    {
        var context = await CreateContextAsync();

        var route = await CreateRouteAsync(
            context.Client,
            2);

        await CreateClientAsync(
            context.Client,
            route.Id,
            "Cliente A",
            19.451000m,
            -70.701000m);

        await CreateClientAsync(
            context.Client,
            route.Id,
            "Cliente B",
            19.455000m,
            -70.705000m);

        await CreateClientAsync(
            context.Client,
            route.Id,
            "Cliente C",
            19.460000m,
            -70.710000m);

        var response = await OptimizeAsync(
            context.Client,
            route.Id,
            19.450000m,
            -70.700000m);

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        var clients = await GetRouteClientsAsync(
            context.Client,
            route.Id);

        Assert.Equal(3, clients.Count);

        Assert.Equal(
            new int?[] { 1, 2, 3 },
            clients.Select(
                x => x.CollectionRouteOrder)
                .ToArray());
    }

    [Fact]
    public async Task Optimize_ClientWithoutCoordinates_RemainsUnordered()
    {
        var context = await CreateContextAsync();

        var route = await CreateRouteAsync(
            context.Client,
            2);

        var withCoordinates =
            await CreateClientAsync(
                context.Client,
                route.Id,
                "Con coordenadas",
                19.451000m,
                -70.701000m);

        var withoutCoordinates =
            await CreateClientAsync(
                context.Client,
                route.Id,
                "Sin coordenadas");

        var response = await OptimizeAsync(
            context.Client,
            route.Id,
            19.450000m,
            -70.700000m);

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        var clients = await GetRouteClientsAsync(
            context.Client,
            route.Id);

        var positioned =
            clients.Single(
                x => x.Id == withCoordinates.Id);

        var unpositioned =
            clients.Single(
                x => x.Id == withoutCoordinates.Id);

        Assert.Equal(
            1,
            positioned.CollectionRouteOrder);

        Assert.Null(
            unpositioned.CollectionRouteOrder);
    }

    [Fact]
    public async Task Optimize_OnManualRoute_ReturnsBadRequest()
    {
        var context = await CreateContextAsync();

        var route = await CreateRouteAsync(
            context.Client,
            1);

        await CreateClientAsync(
            context.Client,
            route.Id,
            "Cliente Manual",
            19.451000m,
            -70.701000m);

        var response = await OptimizeAsync(
            context.Client,
            route.Id,
            19.450000m,
            -70.700000m);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task Optimize_WithOnlyStartLatitude_ReturnsBadRequest()
    {
        var context = await CreateContextAsync();

        var route = await CreateRouteAsync(
            context.Client,
            2);

        var response =
            await context.Client.PostAsJsonAsync(
                $"/api/collection-routes/{route.Id}/optimize",
                new
                {
                    startLatitude = 19.45m
                });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task Optimize_WithOnlyStartLongitude_ReturnsBadRequest()
    {
        var context = await CreateContextAsync();

        var route = await CreateRouteAsync(
            context.Client,
            2);

        var response =
            await context.Client.PostAsJsonAsync(
                $"/api/collection-routes/{route.Id}/optimize",
                new
                {
                    startLongitude = -70.70m
                });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task Optimize_WithInvalidLatitude_ReturnsBadRequest()
    {
        var context = await CreateContextAsync();

        var route = await CreateRouteAsync(
            context.Client,
            2);

        var response = await OptimizeAsync(
            context.Client,
            route.Id,
            91m,
            -70.70m);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task Optimize_WithInvalidLongitude_ReturnsBadRequest()
    {
        var context = await CreateContextAsync();

        var route = await CreateRouteAsync(
            context.Client,
            2);

        var response = await OptimizeAsync(
            context.Client,
            route.Id,
            19.45m,
            -181m);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task Optimize_WithStartingPoint_SelectsNearestClientFirst()
    {
        var context = await CreateContextAsync();

        var route = await CreateRouteAsync(
            context.Client,
            2);

        var nearest = await CreateClientAsync(
            context.Client,
            route.Id,
            "Nearest",
            19.451000m,
            -70.701000m);

        await CreateClientAsync(
            context.Client,
            route.Id,
            "Medium",
            19.470000m,
            -70.720000m);

        await CreateClientAsync(
            context.Client,
            route.Id,
            "Far",
            19.500000m,
            -70.750000m);

        var response = await OptimizeAsync(
            context.Client,
            route.Id,
            19.450000m,
            -70.700000m);

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        var clients = await GetRouteClientsAsync(
            context.Client,
            route.Id);

        Assert.Equal(
            nearest.Id,
            clients[0].Id);

        Assert.Equal(
            1,
            clients[0].CollectionRouteOrder);
    }

    [Fact]
    public async Task Optimize_RouteFromDifferentTenant_ReturnsNotFound()
    {
        var tenantA = await CreateContextAsync();
        var tenantB = await CreateContextAsync();

        var route = await CreateRouteAsync(
            tenantA.Client,
            2);

        var response = await OptimizeAsync(
            tenantB.Client,
            route.Id,
            19.45m,
            -70.70m);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    // ============================================================
    // MODE CHANGES
    // ============================================================

    [Fact]
    public async Task ChangeOrderMode_ManualToAutomatic_PreservesExistingOrder()
    {
        var context = await CreateContextAsync();

        var route = await CreateRouteAsync(
            context.Client,
            1);

        var clientA = await CreateClientAsync(
            context.Client,
            route.Id,
            "Cliente A");

        var clientB = await CreateClientAsync(
            context.Client,
            route.Id,
            "Cliente B");

        await ReorderAsync(
            context.Client,
            route.Id,
            clientB.Id,
            clientA.Id);

        var updateResponse =
            await UpdateRouteAsync(
                context.Client,
                route.Id,
                route.Name,
                2);

        Assert.Equal(
            HttpStatusCode.OK,
            updateResponse.StatusCode);

        var clients = await GetRouteClientsAsync(
            context.Client,
            route.Id);

        Assert.Equal(clientB.Id, clients[0].Id);
        Assert.Equal(1, clients[0].CollectionRouteOrder);

        Assert.Equal(clientA.Id, clients[1].Id);
        Assert.Equal(2, clients[1].CollectionRouteOrder);
    }

    [Fact]
    public async Task ChangeOrderMode_AutomaticToManual_PreservesOptimizedOrder()
    {
        var context = await CreateContextAsync();

        var route = await CreateRouteAsync(
            context.Client,
            2);

        var clientA = await CreateClientAsync(
            context.Client,
            route.Id,
            "Cliente cercano",
            19.451000m,
            -70.701000m);

        var clientB = await CreateClientAsync(
            context.Client,
            route.Id,
            "Cliente lejano",
            19.480000m,
            -70.730000m);

        var optimizeResponse = await OptimizeAsync(
            context.Client,
            route.Id,
            19.450000m,
            -70.700000m);

        Assert.Equal(
            HttpStatusCode.NoContent,
            optimizeResponse.StatusCode);

        var beforeModeChange =
            await GetRouteClientsAsync(
                context.Client,
                route.Id);

        var beforeOrder =
            beforeModeChange
                .Select(x => new
                {
                    x.Id,
                    x.CollectionRouteOrder
                })
                .ToArray();

        var updateResponse =
            await UpdateRouteAsync(
                context.Client,
                route.Id,
                route.Name,
                1);

        Assert.Equal(
            HttpStatusCode.OK,
            updateResponse.StatusCode);

        var afterModeChange =
            await GetRouteClientsAsync(
                context.Client,
                route.Id);

        Assert.Equal(
            beforeOrder.Select(x => x.Id),
            afterModeChange.Select(x => x.Id));

        Assert.Equal(
            beforeOrder.Select(
                x => x.CollectionRouteOrder),
            afterModeChange.Select(
                x => x.CollectionRouteOrder));

        Assert.Contains(
            afterModeChange,
            x =>
                x.Id == clientA.Id &&
                x.CollectionRouteOrder == 1);

        Assert.Contains(
            afterModeChange,
            x =>
                x.Id == clientB.Id &&
                x.CollectionRouteOrder == 2);
    }

    [Fact]
    public async Task ChangeOrderMode_ManualAutomaticManual_PreservesOrderUntilOptimization()
    {
        var context = await CreateContextAsync();

        var route = await CreateRouteAsync(
            context.Client,
            1);

        var clientA = await CreateClientAsync(
            context.Client,
            route.Id,
            "Cliente A");

        var clientB = await CreateClientAsync(
            context.Client,
            route.Id,
            "Cliente B");

        await ReorderAsync(
            context.Client,
            route.Id,
            clientB.Id,
            clientA.Id);

        var toAutomatic =
            await UpdateRouteAsync(
                context.Client,
                route.Id,
                route.Name,
                2);

        Assert.Equal(
            HttpStatusCode.OK,
            toAutomatic.StatusCode);

        var backToManual =
            await UpdateRouteAsync(
                context.Client,
                route.Id,
                route.Name,
                1);

        Assert.Equal(
            HttpStatusCode.OK,
            backToManual.StatusCode);

        var clients = await GetRouteClientsAsync(
            context.Client,
            route.Id);

        Assert.Equal(clientB.Id, clients[0].Id);
        Assert.Equal(1, clients[0].CollectionRouteOrder);

        Assert.Equal(clientA.Id, clients[1].Id);
        Assert.Equal(2, clients[1].CollectionRouteOrder);
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

    private static async Task<RouteResult>
        CreateRouteAsync(
            HttpClient client,
            int orderMode)
    {
        var response =
            await client.PostAsJsonAsync(
                "/api/collection-routes",
                new
                {
                    name =
                        Unique("Ruta Ordering"),
                    description =
                        "Ruta creada por integration test",
                    orderMode
                });

        response.EnsureSuccessStatusCode();

        using var json =
            JsonDocument.Parse(
                await response.Content
                    .ReadAsStringAsync());

        return new RouteResult
        {
            Id = json.RootElement
                .GetProperty("id")
                .GetGuid(),

            Name = json.RootElement
                .GetProperty("name")
                .GetString()!,

            OrderMode = json.RootElement
                .GetProperty("orderMode")
                .GetInt32()
        };
    }

    private static async Task<ClientResult>
        CreateClientAsync(
            HttpClient client,
            Guid collectionRouteId,
            string firstName,
            decimal? latitude = null,
            decimal? longitude = null)
    {
        var response =
            await client.PostAsJsonAsync(
                "/api/clients",
                new
                {
                    firstName,
                    lastName = "Ordering Test",
                    phone = UniquePhone(),
                    secondaryPhone = (string?)null,
                    identificationType = (string?)null,
                    identification = (string?)null,
                    socialNumber = (string?)null,
                    address = "Santiago",
                    latitude,
                    longitude,
                    collectionRouteId,
                    notes = "Integration test"
                });

        response.EnsureSuccessStatusCode();

        using var json =
            JsonDocument.Parse(
                await response.Content
                    .ReadAsStringAsync());

        return new ClientResult
        {
            Id = json.RootElement
                .GetProperty("id")
                .GetGuid(),

            FirstName = json.RootElement
                .GetProperty("firstName")
                .GetString()!,

            Phone = json.RootElement
                .GetProperty("phone")
                .GetString()!,

            CollectionRouteId =
                GetNullableGuid(
                    json.RootElement,
                    "collectionRouteId"),

            CollectionRouteOrder =
                GetNullableInt(
                    json.RootElement,
                    "collectionRouteOrder"),

            Latitude =
                GetNullableDecimal(
                    json.RootElement,
                    "latitude"),

            Longitude =
                GetNullableDecimal(
                    json.RootElement,
                    "longitude")
        };
    }

    private static async Task<HttpResponseMessage>
        ReorderAsync(
            HttpClient client,
            Guid routeId,
            params Guid[] clientIds)
    {
        return await client.PutAsJsonAsync(
            $"/api/collection-routes/{routeId}/clients/order",
            new
            {
                clientIds
            });
    }

    private static async Task<HttpResponseMessage>
        OptimizeAsync(
            HttpClient client,
            Guid routeId,
            decimal? startLatitude = null,
            decimal? startLongitude = null)
    {
        return await client.PostAsJsonAsync(
            $"/api/collection-routes/{routeId}/optimize",
            new
            {
                startLatitude,
                startLongitude
            });
    }

    private static async Task<List<ClientResult>>
        GetRouteClientsAsync(
            HttpClient client,
            Guid routeId)
    {
        var response =
            await client.GetAsync(
                $"/api/clients" +
                $"?collectionRouteId={routeId}");

        response.EnsureSuccessStatusCode();

        using var json =
            JsonDocument.Parse(
                await response.Content
                    .ReadAsStringAsync());

        return json.RootElement
            .EnumerateArray()
            .Select(MapClient)
            .ToList();
    }

    private static async Task<ClientResult>
        GetClientAsync(
            HttpClient client,
            Guid clientId)
    {
        var response =
            await client.GetAsync(
                $"/api/clients/{clientId}");

        response.EnsureSuccessStatusCode();

        using var json =
            JsonDocument.Parse(
                await response.Content
                    .ReadAsStringAsync());

        return MapClient(
            json.RootElement);
    }

    private static async Task<HttpResponseMessage>
        UpdateClientAsync(
            HttpClient client,
            ClientResult existingClient,
            Guid? collectionRouteId,
            string? firstName = null)
    {
        return await client.PutAsJsonAsync(
            $"/api/clients/{existingClient.Id}",
            new
            {
                firstName =
                    firstName ??
                    existingClient.FirstName,
                lastName =
                    "Ordering Test Updated",
                phone =
                    existingClient.Phone,
                secondaryPhone =
                    (string?)null,
                identificationType =
                    (string?)null,
                identification =
                    (string?)null,
                socialNumber =
                    (string?)null,
                address =
                    "Santiago actualizado",
                latitude =
                    existingClient.Latitude,
                longitude =
                    existingClient.Longitude,
                collectionRouteId,
                notes =
                    "Updated from integration test"
            });
    }

    private static async Task<HttpResponseMessage>
        UpdateRouteAsync(
            HttpClient client,
            Guid routeId,
            string name,
            int orderMode)
    {
        return await client.PutAsJsonAsync(
            $"/api/collection-routes/{routeId}",
            new
            {
                name,
                description =
                    "Updated ordering test",
                orderMode
            });
    }

    private static ClientResult MapClient(
        JsonElement element)
    {
        return new ClientResult
        {
            Id = element
                .GetProperty("id")
                .GetGuid(),

            FirstName = element
                .GetProperty("firstName")
                .GetString()!,

            Phone = element
                .GetProperty("phone")
                .GetString()!,

            CollectionRouteId =
                GetNullableGuid(
                    element,
                    "collectionRouteId"),

            CollectionRouteOrder =
                GetNullableInt(
                    element,
                    "collectionRouteOrder"),

            Latitude =
                GetNullableDecimal(
                    element,
                    "latitude"),

            Longitude =
                GetNullableDecimal(
                    element,
                    "longitude")
        };
    }

    private static Guid? GetNullableGuid(
        JsonElement element,
        string propertyName)
    {
        if (!element.TryGetProperty(
                propertyName,
                out var property))
        {
            return null;
        }

        if (property.ValueKind ==
            JsonValueKind.Null)
        {
            return null;
        }

        return property.GetGuid();
    }

    private static int? GetNullableInt(
        JsonElement element,
        string propertyName)
    {
        if (!element.TryGetProperty(
                propertyName,
                out var property))
        {
            return null;
        }

        if (property.ValueKind ==
            JsonValueKind.Null)
        {
            return null;
        }

        return property.GetInt32();
    }

    private static decimal? GetNullableDecimal(
        JsonElement element,
        string propertyName)
    {
        if (!element.TryGetProperty(
                propertyName,
                out var property))
        {
            return null;
        }

        if (property.ValueKind ==
            JsonValueKind.Null)
        {
            return null;
        }

        return property.GetDecimal();
    }

    private static string Unique(
        string prefix)
    {
        return
            $"{prefix} {Guid.NewGuid():N}";
    }

    private static string UniquePhone()
    {
        var value =
            Random.Shared.Next(
                1000000,
                9999999);

        return $"809{value}";
    }

    private sealed class RouteResult
    {
        public Guid Id { get; set; }

        public string Name { get; set; } =
            string.Empty;

        public int OrderMode { get; set; }
    }

    private sealed class ClientResult
    {
        public Guid Id { get; set; }

        public string FirstName { get; set; } =
            string.Empty;

        public string Phone { get; set; } =
            string.Empty;

        public Guid? CollectionRouteId { get; set; }

        public int? CollectionRouteOrder { get; set; }

        public decimal? Latitude { get; set; }

        public decimal? Longitude { get; set; }
    }
}