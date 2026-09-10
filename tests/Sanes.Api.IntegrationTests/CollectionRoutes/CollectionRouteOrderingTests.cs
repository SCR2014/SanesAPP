using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Sanes.Api.IntegrationTests.CollectionRoutes;

public class CollectionRouteOrderingTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CollectionRouteOrderingTests(
        CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ============================================================
    // ORDER MODE
    // ============================================================

    [Fact]
    public async Task Create_WithManualOrderMode_ReturnsManual()
    {
        var tenantId = await CreateTenantAsync();

        var route = await CreateRouteAsync(
            tenantId,
            orderMode: 1);

        Assert.Equal(1, route.OrderMode);
    }

    [Fact]
    public async Task Create_WithAutomaticOrderMode_ReturnsAutomatic()
    {
        var tenantId = await CreateTenantAsync();

        var route = await CreateRouteAsync(
            tenantId,
            orderMode: 2);

        Assert.Equal(2, route.OrderMode);
    }

    [Fact]
    public async Task Create_WithInvalidOrderMode_ReturnsBadRequest()
    {
        var tenantId = await CreateTenantAsync();

        var response = await _client.PostAsJsonAsync(
            "/api/collection-routes",
            new
            {
                tenantId,
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
        var tenantId = await CreateTenantAsync();

        var route = await CreateRouteAsync(
            tenantId,
            orderMode: 1);

        var clientA = await CreateClientAsync(
            tenantId,
            route.Id,
            "Cliente A");

        var clientB = await CreateClientAsync(
            tenantId,
            route.Id,
            "Cliente B");

        var clientC = await CreateClientAsync(
            tenantId,
            route.Id,
            "Cliente C");

        var response = await ReorderAsync(
            tenantId,
            route.Id,
            clientC.Id,
            clientA.Id,
            clientB.Id);

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        var clients = await GetRouteClientsAsync(
            tenantId,
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
        var tenantId = await CreateTenantAsync();

        var route = await CreateRouteAsync(
            tenantId,
            orderMode: 1);

        var clientA = await CreateClientAsync(
            tenantId,
            route.Id,
            "Cliente A");

        var clientB = await CreateClientAsync(
            tenantId,
            route.Id,
            "Cliente B");

        var clientC = await CreateClientAsync(
            tenantId,
            route.Id,
            "Cliente C");

        var firstOrder = await ReorderAsync(
            tenantId,
            route.Id,
            clientA.Id,
            clientB.Id,
            clientC.Id);

        Assert.Equal(
            HttpStatusCode.NoContent,
            firstOrder.StatusCode);

        var secondOrder = await ReorderAsync(
            tenantId,
            route.Id,
            clientB.Id,
            clientA.Id);

        Assert.Equal(
            HttpStatusCode.NoContent,
            secondOrder.StatusCode);

        var clients = await GetRouteClientsAsync(
            tenantId,
            route.Id);

        var omittedClient =
            clients.Single(x => x.Id == clientC.Id);

        Assert.Null(
            omittedClient.CollectionRouteOrder);
    }

    [Fact]
    public async Task ReorderClients_WithDuplicateIds_ReturnsBadRequest()
    {
        var tenantId = await CreateTenantAsync();

        var route = await CreateRouteAsync(
            tenantId,
            orderMode: 1);

        var client = await CreateClientAsync(
            tenantId,
            route.Id,
            "Cliente duplicado");

        var response = await ReorderAsync(
            tenantId,
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
        var tenantId = await CreateTenantAsync();

        var routeA = await CreateRouteAsync(
            tenantId,
            orderMode: 1);

        var routeB = await CreateRouteAsync(
            tenantId,
            orderMode: 1);

        var clientA = await CreateClientAsync(
            tenantId,
            routeA.Id,
            "Cliente Ruta A");

        var clientB = await CreateClientAsync(
            tenantId,
            routeB.Id,
            "Cliente Ruta B");

        var response = await ReorderAsync(
            tenantId,
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
        var tenantA = await CreateTenantAsync();
        var tenantB = await CreateTenantAsync();

        var routeA = await CreateRouteAsync(
            tenantA,
            orderMode: 1);

        var routeB = await CreateRouteAsync(
            tenantB,
            orderMode: 1);

        var clientA = await CreateClientAsync(
            tenantA,
            routeA.Id,
            "Cliente Tenant A");

        var clientB = await CreateClientAsync(
            tenantB,
            routeB.Id,
            "Cliente Tenant B");

        var response = await ReorderAsync(
            tenantA,
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
        var tenantId = await CreateTenantAsync();

        var route = await CreateRouteAsync(
            tenantId,
            orderMode: 2);

        var client = await CreateClientAsync(
            tenantId,
            route.Id,
            "Cliente Automatic");

        var response = await ReorderAsync(
            tenantId,
            route.Id,
            client.Id);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task ReorderClients_RouteFromDifferentTenant_ReturnsNotFound()
    {
        var tenantA = await CreateTenantAsync();
        var tenantB = await CreateTenantAsync();

        var route = await CreateRouteAsync(
            tenantA,
            orderMode: 1);

        var response = await ReorderAsync(
            tenantB,
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
        var tenantId = await CreateTenantAsync();

        var route = await CreateRouteAsync(
            tenantId,
            orderMode: 1);

        var clientA = await CreateClientAsync(
            tenantId,
            route.Id,
            "AAA");

        var clientB = await CreateClientAsync(
            tenantId,
            route.Id,
            "BBB");

        var clientC = await CreateClientAsync(
            tenantId,
            route.Id,
            "CCC");

        await ReorderAsync(
            tenantId,
            route.Id,
            clientC.Id,
            clientA.Id,
            clientB.Id);

        var clients = await GetRouteClientsAsync(
            tenantId,
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
        var tenantId = await CreateTenantAsync();

        var route = await CreateRouteAsync(
            tenantId,
            orderMode: 1);

        var orderedA = await CreateClientAsync(
            tenantId,
            route.Id,
            "ZZZ Ordered A");

        var orderedB = await CreateClientAsync(
            tenantId,
            route.Id,
            "YYY Ordered B");

        var unordered = await CreateClientAsync(
            tenantId,
            route.Id,
            "AAA Unordered");

        await ReorderAsync(
            tenantId,
            route.Id,
            orderedB.Id,
            orderedA.Id);

        var clients = await GetRouteClientsAsync(
            tenantId,
            route.Id);

        Assert.Equal(orderedB.Id, clients[0].Id);
        Assert.Equal(orderedA.Id, clients[1].Id);
        Assert.Equal(unordered.Id, clients[2].Id);

        Assert.Equal(1, clients[0].CollectionRouteOrder);
        Assert.Equal(2, clients[1].CollectionRouteOrder);
        Assert.Null(clients[2].CollectionRouteOrder);
    }

    // ============================================================
    // CLIENT MOVEMENT BETWEEN ROUTES
    // ============================================================

    [Fact]
    public async Task UpdateClient_MovingToDifferentRoute_ClearsOrder()
    {
        var tenantId = await CreateTenantAsync();

        var routeA = await CreateRouteAsync(
            tenantId,
            orderMode: 1);

        var routeB = await CreateRouteAsync(
            tenantId,
            orderMode: 1);

        var client = await CreateClientAsync(
            tenantId,
            routeA.Id,
            "Cliente mover");

        await ReorderAsync(
            tenantId,
            routeA.Id,
            client.Id);

        var updateResponse = await UpdateClientAsync(
            tenantId,
            client,
            routeB.Id);

        Assert.Equal(
            HttpStatusCode.OK,
            updateResponse.StatusCode);

        var updatedClient =
            await GetClientAsync(
                tenantId,
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
        var tenantId = await CreateTenantAsync();

        var route = await CreateRouteAsync(
            tenantId,
            orderMode: 1);

        var client = await CreateClientAsync(
            tenantId,
            route.Id,
            "Cliente conserva");

        await ReorderAsync(
            tenantId,
            route.Id,
            client.Id);

        var updateResponse = await UpdateClientAsync(
            tenantId,
            client,
            route.Id,
            firstName: "Cliente actualizado");

        Assert.Equal(
            HttpStatusCode.OK,
            updateResponse.StatusCode);

        var updatedClient =
            await GetClientAsync(
                tenantId,
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
        var tenantId = await CreateTenantAsync();

        var route = await CreateRouteAsync(
            tenantId,
            orderMode: 2);

        await CreateClientAsync(
            tenantId,
            route.Id,
            "Cliente A",
            19.451000m,
            -70.701000m);

        await CreateClientAsync(
            tenantId,
            route.Id,
            "Cliente B",
            19.455000m,
            -70.705000m);

        await CreateClientAsync(
            tenantId,
            route.Id,
            "Cliente C",
            19.460000m,
            -70.710000m);

        var response = await OptimizeAsync(
            tenantId,
            route.Id,
            19.450000m,
            -70.700000m);

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        var clients = await GetRouteClientsAsync(
            tenantId,
            route.Id);

        Assert.Equal(3, clients.Count);

        Assert.Equal(
            new int?[] { 1, 2, 3 },
            clients.Select(
                x => x.CollectionRouteOrder).ToArray());
    }

    [Fact]
    public async Task Optimize_ClientWithoutCoordinates_RemainsUnordered()
    {
        var tenantId = await CreateTenantAsync();

        var route = await CreateRouteAsync(
            tenantId,
            orderMode: 2);

        var withCoordinates =
            await CreateClientAsync(
                tenantId,
                route.Id,
                "Con coordenadas",
                19.451000m,
                -70.701000m);

        var withoutCoordinates =
            await CreateClientAsync(
                tenantId,
                route.Id,
                "Sin coordenadas");

        var response = await OptimizeAsync(
            tenantId,
            route.Id,
            19.450000m,
            -70.700000m);

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        var clients = await GetRouteClientsAsync(
            tenantId,
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
        var tenantId = await CreateTenantAsync();

        var route = await CreateRouteAsync(
            tenantId,
            orderMode: 1);

        await CreateClientAsync(
            tenantId,
            route.Id,
            "Cliente Manual",
            19.451000m,
            -70.701000m);

        var response = await OptimizeAsync(
            tenantId,
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
        var tenantId = await CreateTenantAsync();

        var route = await CreateRouteAsync(
            tenantId,
            orderMode: 2);

        var response = await _client.PostAsJsonAsync(
            $"/api/collection-routes/{route.Id}/optimize?tenantId={tenantId}",
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
        var tenantId = await CreateTenantAsync();

        var route = await CreateRouteAsync(
            tenantId,
            orderMode: 2);

        var response = await _client.PostAsJsonAsync(
            $"/api/collection-routes/{route.Id}/optimize?tenantId={tenantId}",
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
        var tenantId = await CreateTenantAsync();

        var route = await CreateRouteAsync(
            tenantId,
            orderMode: 2);

        var response = await OptimizeAsync(
            tenantId,
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
        var tenantId = await CreateTenantAsync();

        var route = await CreateRouteAsync(
            tenantId,
            orderMode: 2);

        var response = await OptimizeAsync(
            tenantId,
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
        var tenantId = await CreateTenantAsync();

        var route = await CreateRouteAsync(
            tenantId,
            orderMode: 2);

        var nearest = await CreateClientAsync(
            tenantId,
            route.Id,
            "Nearest",
            19.451000m,
            -70.701000m);

        await CreateClientAsync(
            tenantId,
            route.Id,
            "Medium",
            19.470000m,
            -70.720000m);

        await CreateClientAsync(
            tenantId,
            route.Id,
            "Far",
            19.500000m,
            -70.750000m);

        var response = await OptimizeAsync(
            tenantId,
            route.Id,
            19.450000m,
            -70.700000m);

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        var clients = await GetRouteClientsAsync(
            tenantId,
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
        var tenantA = await CreateTenantAsync();
        var tenantB = await CreateTenantAsync();

        var route = await CreateRouteAsync(
            tenantA,
            orderMode: 2);

        var response = await OptimizeAsync(
            tenantB,
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
        var tenantId = await CreateTenantAsync();

        var route = await CreateRouteAsync(
            tenantId,
            orderMode: 1);

        var clientA = await CreateClientAsync(
            tenantId,
            route.Id,
            "Cliente A");

        var clientB = await CreateClientAsync(
            tenantId,
            route.Id,
            "Cliente B");

        await ReorderAsync(
            tenantId,
            route.Id,
            clientB.Id,
            clientA.Id);

        var updateResponse =
            await UpdateRouteAsync(
                tenantId,
                route.Id,
                route.Name,
                2);

        Assert.Equal(
            HttpStatusCode.OK,
            updateResponse.StatusCode);

        var clients = await GetRouteClientsAsync(
            tenantId,
            route.Id);

        Assert.Equal(clientB.Id, clients[0].Id);
        Assert.Equal(1, clients[0].CollectionRouteOrder);

        Assert.Equal(clientA.Id, clients[1].Id);
        Assert.Equal(2, clients[1].CollectionRouteOrder);
    }

    [Fact]
    public async Task ChangeOrderMode_AutomaticToManual_PreservesOptimizedOrder()
    {
        var tenantId = await CreateTenantAsync();

        var route = await CreateRouteAsync(
            tenantId,
            orderMode: 2);

        var clientA = await CreateClientAsync(
            tenantId,
            route.Id,
            "Cliente cercano",
            19.451000m,
            -70.701000m);

        var clientB = await CreateClientAsync(
            tenantId,
            route.Id,
            "Cliente lejano",
            19.480000m,
            -70.730000m);

        var optimizeResponse = await OptimizeAsync(
            tenantId,
            route.Id,
            19.450000m,
            -70.700000m);

        Assert.Equal(
            HttpStatusCode.NoContent,
            optimizeResponse.StatusCode);

        var beforeModeChange =
            await GetRouteClientsAsync(
                tenantId,
                route.Id);

        var beforeOrder = beforeModeChange
            .Select(x => new
            {
                x.Id,
                x.CollectionRouteOrder
            })
            .ToArray();

        var updateResponse =
            await UpdateRouteAsync(
                tenantId,
                route.Id,
                route.Name,
                1);

        Assert.Equal(
            HttpStatusCode.OK,
            updateResponse.StatusCode);

        var afterModeChange =
            await GetRouteClientsAsync(
                tenantId,
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
        var tenantId = await CreateTenantAsync();

        var route = await CreateRouteAsync(
            tenantId,
            orderMode: 1);

        var clientA = await CreateClientAsync(
            tenantId,
            route.Id,
            "Cliente A");

        var clientB = await CreateClientAsync(
            tenantId,
            route.Id,
            "Cliente B");

        await ReorderAsync(
            tenantId,
            route.Id,
            clientB.Id,
            clientA.Id);

        var toAutomatic =
            await UpdateRouteAsync(
                tenantId,
                route.Id,
                route.Name,
                2);

        Assert.Equal(
            HttpStatusCode.OK,
            toAutomatic.StatusCode);

        var backToManual =
            await UpdateRouteAsync(
                tenantId,
                route.Id,
                route.Name,
                1);

        Assert.Equal(
            HttpStatusCode.OK,
            backToManual.StatusCode);

        var clients = await GetRouteClientsAsync(
            tenantId,
            route.Id);

        Assert.Equal(clientB.Id, clients[0].Id);
        Assert.Equal(1, clients[0].CollectionRouteOrder);

        Assert.Equal(clientA.Id, clients[1].Id);
        Assert.Equal(2, clients[1].CollectionRouteOrder);
    }

    // ============================================================
    // HELPERS
    // ============================================================

    private async Task<Guid> CreateTenantAsync()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/tenants",
            new
            {
                name = Unique("Tenant Ordering"),
                legalName = "Sanes Test SRL",
                phone = UniquePhone(),
                email = $"{Guid.NewGuid():N}@example.com",
                currencyCode = "DOP",
                currencySymbol = "RD$"
            });

        response.EnsureSuccessStatusCode();

        using var json =
            JsonDocument.Parse(
                await response.Content.ReadAsStringAsync());

        return json.RootElement
            .GetProperty("id")
            .GetGuid();
    }

    private async Task<RouteResult> CreateRouteAsync(
        Guid tenantId,
        int orderMode)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/collection-routes",
            new
            {
                tenantId,
                name = Unique("Ruta Ordering"),
                description = "Ruta creada por integration test",
                orderMode
            });

        response.EnsureSuccessStatusCode();

        using var json =
            JsonDocument.Parse(
                await response.Content.ReadAsStringAsync());

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

    private async Task<ClientResult> CreateClientAsync(
        Guid tenantId,
        Guid collectionRouteId,
        string firstName,
        decimal? latitude = null,
        decimal? longitude = null)
    {
        var phone = UniquePhone();

        var response = await _client.PostAsJsonAsync(
            "/api/clients",
            new
            {
                tenantId,
                firstName,
                lastName = "Ordering Test",
                phone,
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
                await response.Content.ReadAsStringAsync());

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

    private async Task<HttpResponseMessage> ReorderAsync(
        Guid tenantId,
        Guid routeId,
        params Guid[] clientIds)
    {
        return await _client.PutAsJsonAsync(
            $"/api/collection-routes/{routeId}/clients/order?tenantId={tenantId}",
            new
            {
                clientIds
            });
    }

    private async Task<HttpResponseMessage> OptimizeAsync(
        Guid tenantId,
        Guid routeId,
        decimal? startLatitude = null,
        decimal? startLongitude = null)
    {
        return await _client.PostAsJsonAsync(
            $"/api/collection-routes/{routeId}/optimize?tenantId={tenantId}",
            new
            {
                startLatitude,
                startLongitude
            });
    }

    private async Task<List<ClientResult>> GetRouteClientsAsync(
        Guid tenantId,
        Guid routeId)
    {
        var response = await _client.GetAsync(
            $"/api/clients?tenantId={tenantId}&collectionRouteId={routeId}");

        response.EnsureSuccessStatusCode();

        using var json =
            JsonDocument.Parse(
                await response.Content.ReadAsStringAsync());

        return json.RootElement
            .EnumerateArray()
            .Select(MapClient)
            .ToList();
    }

    private async Task<ClientResult> GetClientAsync(
        Guid tenantId,
        Guid clientId)
    {
        var response = await _client.GetAsync(
            $"/api/clients/{clientId}?tenantId={tenantId}");

        response.EnsureSuccessStatusCode();

        using var json =
            JsonDocument.Parse(
                await response.Content.ReadAsStringAsync());

        return MapClient(json.RootElement);
    }

    private async Task<HttpResponseMessage> UpdateClientAsync(
        Guid tenantId,
        ClientResult client,
        Guid? collectionRouteId,
        string? firstName = null)
    {
        return await _client.PutAsJsonAsync(
            $"/api/clients/{client.Id}?tenantId={tenantId}",
            new
            {
                firstName = firstName ?? client.FirstName,
                lastName = "Ordering Test Updated",
                phone = client.Phone,
                secondaryPhone = (string?)null,
                identificationType = (string?)null,
                identification = (string?)null,
                socialNumber = (string?)null,
                address = "Santiago actualizado",
                latitude = client.Latitude,
                longitude = client.Longitude,
                collectionRouteId,
                notes = "Updated from integration test"
            });
    }

    private async Task<HttpResponseMessage> UpdateRouteAsync(
        Guid tenantId,
        Guid routeId,
        string name,
        int orderMode)
    {
        return await _client.PutAsJsonAsync(
            $"/api/collection-routes/{routeId}?tenantId={tenantId}",
            new
            {
                name,
                description = "Updated ordering test",
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
        return $"{prefix} {Guid.NewGuid():N}";
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

        public string Name { get; set; }
            = string.Empty;

        public int OrderMode { get; set; }
    }

    private sealed class ClientResult
    {
        public Guid Id { get; set; }

        public string FirstName { get; set; }
            = string.Empty;

        public string Phone { get; set; }
            = string.Empty;

        public Guid? CollectionRouteId { get; set; }

        public int? CollectionRouteOrder { get; set; }

        public decimal? Latitude { get; set; }

        public decimal? Longitude { get; set; }
    }
}