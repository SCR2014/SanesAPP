using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Sanes.Application.CollectionAgenda.DTOs;
using Sanes.Application.CollectionRouteSchedules.DTOs;
using Sanes.Domain.Enums;

namespace Sanes.Api.IntegrationTests.CollectionAgenda;

public class CollectionAgendaTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CollectionAgendaTests(
        CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAgenda_ForScheduledDay_ReturnsRoute()
    {
        var tenantId = await CreateTenantAsync();
        var routeId = await CreateRouteAsync(tenantId);

        await CreateScheduleAsync(
            tenantId,
            routeId,
            CollectionWeekDay.Monday);

        var response = await GetAgendaAsync(
            tenantId,
            new DateOnly(2026, 9, 14));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var agenda =
            await response.Content
                .ReadFromJsonAsync<CollectionAgendaResponse>();

        Assert.NotNull(agenda);
        Assert.Equal(CollectionWeekDay.Monday, agenda.DayOfWeek);
        Assert.Equal(1, agenda.RoutesCount);
        Assert.Single(agenda.Routes);
        Assert.Equal(routeId, agenda.Routes[0].CollectionRouteId);
    }

    [Fact]
    public async Task GetAgenda_ForUnscheduledDay_ReturnsEmptyAgenda()
    {
        var tenantId = await CreateTenantAsync();
        var routeId = await CreateRouteAsync(tenantId);

        await CreateScheduleAsync(
            tenantId,
            routeId,
            CollectionWeekDay.Monday);

        var response = await GetAgendaAsync(
            tenantId,
            new DateOnly(2026, 9, 15));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var agenda =
            await response.Content
                .ReadFromJsonAsync<CollectionAgendaResponse>();

        Assert.NotNull(agenda);
        Assert.Equal(CollectionWeekDay.Tuesday, agenda.DayOfWeek);
        Assert.Equal(0, agenda.RoutesCount);
        Assert.Empty(agenda.Routes);
    }

    [Fact]
    public async Task GetAgenda_ReturnsScheduleTime()
    {
        var tenantId = await CreateTenantAsync();
        var routeId = await CreateRouteAsync(tenantId);

        await CreateScheduleAsync(
            tenantId,
            routeId,
            CollectionWeekDay.Monday,
            new TimeOnly(8, 0),
            new TimeOnly(13, 0));

        var response = await GetAgendaAsync(
            tenantId,
            new DateOnly(2026, 9, 14));

        var agenda =
            await response.Content
                .ReadFromJsonAsync<CollectionAgendaResponse>();

        Assert.NotNull(agenda);

        var route = Assert.Single(agenda.Routes);

        Assert.Equal(new TimeOnly(8, 0), route.StartTime);
        Assert.Equal(new TimeOnly(13, 0), route.EndTime);
    }

    [Fact]
    public async Task GetAgenda_IncludesAssignedCollector()
    {
        var tenantId = await CreateTenantAsync();
        var routeId = await CreateRouteAsync(tenantId);
        var collectorId = await CreateCollectorAsync(tenantId);

        await AssignRouteAsync(
            tenantId,
            collectorId,
            routeId);

        await CreateScheduleAsync(
            tenantId,
            routeId,
            CollectionWeekDay.Monday);

        var response = await GetAgendaAsync(
            tenantId,
            new DateOnly(2026, 9, 14));

        var agenda =
            await response.Content
                .ReadFromJsonAsync<CollectionAgendaResponse>();

        Assert.NotNull(agenda);

        var route = Assert.Single(agenda.Routes);
        var collector = Assert.Single(route.Collectors);

        Assert.Equal(collectorId, collector.AppUserId);
        Assert.Equal(1, agenda.CollectorsCount);
    }

    [Fact]
    public async Task GetAgenda_WithoutCollectorStillReturnsScheduledRoute()
    {
        var tenantId = await CreateTenantAsync();
        var routeId = await CreateRouteAsync(tenantId);

        await CreateScheduleAsync(
            tenantId,
            routeId,
            CollectionWeekDay.Monday);

        var response = await GetAgendaAsync(
            tenantId,
            new DateOnly(2026, 9, 14));

        var agenda =
            await response.Content
                .ReadFromJsonAsync<CollectionAgendaResponse>();

        Assert.NotNull(agenda);

        var route = Assert.Single(agenda.Routes);

        Assert.Empty(route.Collectors);
        Assert.Equal(1, agenda.RoutesCount);
        Assert.Equal(0, agenda.CollectorsCount);
    }

    [Fact]
    public async Task GetAgenda_FilterByCollector_ReturnsOnlyAssignedRoutes()
    {
        var tenantId = await CreateTenantAsync();

        var route1 = await CreateRouteAsync(tenantId);
        var route2 = await CreateRouteAsync(tenantId);

        var collector1 = await CreateCollectorAsync(tenantId);
        var collector2 = await CreateCollectorAsync(tenantId);

        await AssignRouteAsync(
            tenantId,
            collector1,
            route1);

        await AssignRouteAsync(
            tenantId,
            collector2,
            route2);

        await CreateScheduleAsync(
            tenantId,
            route1,
            CollectionWeekDay.Monday);

        await CreateScheduleAsync(
            tenantId,
            route2,
            CollectionWeekDay.Monday);

        var response = await GetAgendaAsync(
            tenantId,
            new DateOnly(2026, 9, 14),
            collector1);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var agenda =
            await response.Content
                .ReadFromJsonAsync<CollectionAgendaResponse>();

        Assert.NotNull(agenda);
        Assert.Equal(1, agenda.RoutesCount);

        var route = Assert.Single(agenda.Routes);

        Assert.Equal(route1, route.CollectionRouteId);
        Assert.Single(route.Collectors);
        Assert.Equal(
            collector1,
            route.Collectors[0].AppUserId);
    }

    [Fact]
    public async Task GetAgenda_FilterByCollector_WithNoAssignedRoute_ReturnsEmpty()
    {
        var tenantId = await CreateTenantAsync();

        var routeId = await CreateRouteAsync(tenantId);
        var collectorId = await CreateCollectorAsync(tenantId);

        await CreateScheduleAsync(
            tenantId,
            routeId,
            CollectionWeekDay.Monday);

        var response = await GetAgendaAsync(
            tenantId,
            new DateOnly(2026, 9, 14),
            collectorId);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var agenda =
            await response.Content
                .ReadFromJsonAsync<CollectionAgendaResponse>();

        Assert.NotNull(agenda);
        Assert.Equal(0, agenda.RoutesCount);
        Assert.Empty(agenda.Routes);
    }

    [Fact]
    public async Task GetAgenda_FilterByAdministrator_ReturnsBadRequest()
    {
        var tenantId = await CreateTenantAsync();

        var administratorId =
            await CreateAppUserAsync(
                tenantId,
                AppUserRole.Administrator);

        var response = await GetAgendaAsync(
            tenantId,
            new DateOnly(2026, 9, 14),
            administratorId);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task GetAgenda_FilterByUserFromAnotherTenant_ReturnsBadRequest()
    {
        var tenant1 = await CreateTenantAsync();
        var tenant2 = await CreateTenantAsync();

        var collectorId =
            await CreateCollectorAsync(tenant1);

        var response = await GetAgendaAsync(
            tenant2,
            new DateOnly(2026, 9, 14),
            collectorId);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task GetAgenda_DoesNotReturnRouteFromAnotherTenant()
    {
        var tenant1 = await CreateTenantAsync();
        var tenant2 = await CreateTenantAsync();

        var route1 = await CreateRouteAsync(tenant1);
        var route2 = await CreateRouteAsync(tenant2);

        await CreateScheduleAsync(
            tenant1,
            route1,
            CollectionWeekDay.Monday);

        await CreateScheduleAsync(
            tenant2,
            route2,
            CollectionWeekDay.Monday);

        var response = await GetAgendaAsync(
            tenant1,
            new DateOnly(2026, 9, 14));

        var agenda =
            await response.Content
                .ReadFromJsonAsync<CollectionAgendaResponse>();

        Assert.NotNull(agenda);
        Assert.Equal(1, agenda.RoutesCount);

        Assert.DoesNotContain(
            agenda.Routes,
            x => x.CollectionRouteId == route2);
    }

    [Fact]
    public async Task GetAgenda_IncludesClientsAssignedToRoute()
    {
        var tenantId = await CreateTenantAsync();
        var routeId = await CreateRouteAsync(tenantId);

        var clientId = await CreateClientAsync(
            tenantId,
            routeId,
            "Agenda",
            "Client");

        await CreateScheduleAsync(
            tenantId,
            routeId,
            CollectionWeekDay.Monday);

        var response = await GetAgendaAsync(
            tenantId,
            new DateOnly(2026, 9, 14));

        var agenda =
            await response.Content
                .ReadFromJsonAsync<CollectionAgendaResponse>();

        Assert.NotNull(agenda);

        var route = Assert.Single(agenda.Routes);
        var client = Assert.Single(route.Clients);

        Assert.Equal(clientId, client.ClientId);
        Assert.Equal("Agenda", client.FirstName);
        Assert.Equal("Client", client.LastName);
        Assert.Equal(1, agenda.ClientsCount);
    }

    [Fact]
    public async Task GetAgenda_DoesNotIncludeClientFromAnotherTenant()
    {
        var tenant1 = await CreateTenantAsync();
        var tenant2 = await CreateTenantAsync();

        var route1 = await CreateRouteAsync(tenant1);
        var route2 = await CreateRouteAsync(tenant2);

        var client2 = await CreateClientAsync(
            tenant2,
            route2,
            "Other",
            "Tenant");

        await CreateScheduleAsync(
            tenant1,
            route1,
            CollectionWeekDay.Monday);

        await CreateScheduleAsync(
            tenant2,
            route2,
            CollectionWeekDay.Monday);

        var response = await GetAgendaAsync(
            tenant1,
            new DateOnly(2026, 9, 14));

        var agenda =
            await response.Content
                .ReadFromJsonAsync<CollectionAgendaResponse>();

        Assert.NotNull(agenda);

        Assert.DoesNotContain(
            agenda.Routes.SelectMany(x => x.Clients),
            x => x.ClientId == client2);
    }

    [Fact]
    public async Task GetAgenda_OrdersClientsByCollectionRouteOrder()
    {
        var tenantId = await CreateTenantAsync();
        var routeId = await CreateRouteAsync(tenantId);

        var client1 = await CreateClientAsync(
            tenantId,
            routeId,
            "First",
            "Client");

        var client2 = await CreateClientAsync(
            tenantId,
            routeId,
            "Second",
            "Client");

        var client3 = await CreateClientAsync(
            tenantId,
            routeId,
            "Third",
            "Client");

        await SetManualOrderAsync(
            tenantId,
            routeId,
            new[]
            {
                client3,
                client1,
                client2
            });

        await CreateScheduleAsync(
            tenantId,
            routeId,
            CollectionWeekDay.Monday);

        var response = await GetAgendaAsync(
            tenantId,
            new DateOnly(2026, 9, 14));

        var agenda =
            await response.Content
                .ReadFromJsonAsync<CollectionAgendaResponse>();

        Assert.NotNull(agenda);

        var route = Assert.Single(agenda.Routes);

        Assert.Equal(3, route.Clients.Count);

        Assert.Equal(client3, route.Clients[0].ClientId);
        Assert.Equal(client1, route.Clients[1].ClientId);
        Assert.Equal(client2, route.Clients[2].ClientId);

        Assert.Equal(1, route.Clients[0].CollectionRouteOrder);
        Assert.Equal(2, route.Clients[1].CollectionRouteOrder);
        Assert.Equal(3, route.Clients[2].CollectionRouteOrder);
    }

    [Fact]
    public async Task GetAgenda_SoftDeletedScheduleIsNotReturned()
    {
        var tenantId = await CreateTenantAsync();
        var routeId = await CreateRouteAsync(tenantId);

        var schedule = await CreateScheduleAsync(
            tenantId,
            routeId,
            CollectionWeekDay.Monday);

        var deleteResponse = await _client.DeleteAsync(
            $"/api/collection-route-schedules/{schedule.Id}" +
            $"?tenantId={tenantId}" +
            $"&collectionRouteId={routeId}");

        Assert.Equal(
            HttpStatusCode.NoContent,
            deleteResponse.StatusCode);

        var response = await GetAgendaAsync(
            tenantId,
            new DateOnly(2026, 9, 14));

        var agenda =
            await response.Content
                .ReadFromJsonAsync<CollectionAgendaResponse>();

        Assert.NotNull(agenda);
        Assert.Equal(0, agenda.RoutesCount);
    }

    [Fact]
    public async Task GetAgenda_InactiveCollectorIsNotReturned()
    {
        var tenantId = await CreateTenantAsync();
        var routeId = await CreateRouteAsync(tenantId);
        var collectorId = await CreateCollectorAsync(tenantId);

        await AssignRouteAsync(
            tenantId,
            collectorId,
            routeId);

        await CreateScheduleAsync(
            tenantId,
            routeId,
            CollectionWeekDay.Monday);

        var deleteUserResponse =
            await _client.DeleteAsync(
                $"/api/app-users/{collectorId}" +
                $"?tenantId={tenantId}");

        Assert.Equal(
            HttpStatusCode.NoContent,
            deleteUserResponse.StatusCode);

        var response = await GetAgendaAsync(
            tenantId,
            new DateOnly(2026, 9, 14));

        var agenda =
            await response.Content
                .ReadFromJsonAsync<CollectionAgendaResponse>();

        Assert.NotNull(agenda);

        var route = Assert.Single(agenda.Routes);

        Assert.Empty(route.Collectors);
        Assert.Equal(0, agenda.CollectorsCount);
    }

    [Fact]
    public async Task GetAgenda_ReturnsDistinctCollectorCount()
    {
        var tenantId = await CreateTenantAsync();

        var route1 = await CreateRouteAsync(tenantId);
        var route2 = await CreateRouteAsync(tenantId);

        var collectorId = await CreateCollectorAsync(tenantId);

        await AssignRouteAsync(
            tenantId,
            collectorId,
            route1);

        await AssignRouteAsync(
            tenantId,
            collectorId,
            route2);

        await CreateScheduleAsync(
            tenantId,
            route1,
            CollectionWeekDay.Monday);

        await CreateScheduleAsync(
            tenantId,
            route2,
            CollectionWeekDay.Monday);

        var response = await GetAgendaAsync(
            tenantId,
            new DateOnly(2026, 9, 14));

        var agenda =
            await response.Content
                .ReadFromJsonAsync<CollectionAgendaResponse>();

        Assert.NotNull(agenda);
        Assert.Equal(2, agenda.RoutesCount);
        Assert.Equal(1, agenda.CollectorsCount);
    }

    private async Task<HttpResponseMessage> GetAgendaAsync(
        Guid tenantId,
        DateOnly date,
        Guid? appUserId = null)
    {
        var url =
            $"/api/collection-agenda" +
            $"?tenantId={tenantId}" +
            $"&date={date:yyyy-MM-dd}";

        if (appUserId.HasValue)
        {
            url += $"&appUserId={appUserId.Value}";
        }

        return await _client.GetAsync(url);
    }

    private async Task<Guid> CreateTenantAsync()
    {
        var suffix = Guid.NewGuid().ToString("N");

        var request = new
        {
            name = $"Agenda Tenant {suffix}",
            legalName = $"Agenda Tenant {suffix}",
            phone = "8095551234",
            email = $"agenda-{suffix}@example.com",
            currencyCode = "DOP",
            currencySymbol = "RD$"
        };

        var response = await _client.PostAsJsonAsync(
            "/api/tenants",
            request);

        response.EnsureSuccessStatusCode();

        var json =
            await response.Content
                .ReadFromJsonAsync<JsonElement>();

        return json.GetProperty("id").GetGuid();
    }

    private async Task<Guid> CreateRouteAsync(
        Guid tenantId)
    {
        var suffix = Guid.NewGuid().ToString("N");

        var request = new
        {
            tenantId,
            name = $"Agenda Route {suffix}",
            description = "Agenda integration test route",
            orderMode = 1
        };

        var response = await _client.PostAsJsonAsync(
            "/api/collection-routes",
            request);

        response.EnsureSuccessStatusCode();

        var json =
            await response.Content
                .ReadFromJsonAsync<JsonElement>();

        return json.GetProperty("id").GetGuid();
    }

    private async Task<CollectionRouteScheduleResponse>
        CreateScheduleAsync(
            Guid tenantId,
            Guid routeId,
            CollectionWeekDay day,
            TimeOnly? startTime = null,
            TimeOnly? endTime = null)
    {
        var request = new CreateCollectionRouteScheduleRequest
        {
            TenantId = tenantId,
            CollectionRouteId = routeId,
            DayOfWeek = day,
            StartTime = startTime,
            EndTime = endTime
        };

        var response = await _client.PostAsJsonAsync(
            "/api/collection-route-schedules",
            request);

        response.EnsureSuccessStatusCode();

        var schedule =
            await response.Content
                .ReadFromJsonAsync<CollectionRouteScheduleResponse>();

        Assert.NotNull(schedule);

        return schedule;
    }

    private async Task<Guid> CreateCollectorAsync(
        Guid tenantId)
    {
        return await CreateAppUserAsync(
            tenantId,
            AppUserRole.Collector);
    }

    private async Task<Guid> CreateAppUserAsync(
        Guid tenantId,
        AppUserRole role)
    {
        var suffix = Guid.NewGuid().ToString("N");

        var request = new
        {
            tenantId,
            name = $"Agenda User {suffix}",
            username = $"agenda-{suffix}",
            email = $"agenda-user-{suffix}@example.com",
            phone = "8095551234",
            role = (int)role
        };

        var response = await _client.PostAsJsonAsync(
            "/api/app-users",
            request);

        response.EnsureSuccessStatusCode();

        var json =
            await response.Content
                .ReadFromJsonAsync<JsonElement>();

        return json.GetProperty("id").GetGuid();
    }

    private async Task AssignRouteAsync(
        Guid tenantId,
        Guid collectorId,
        Guid routeId)
    {
        var response = await _client.PostAsync(
            $"/api/app-users/{collectorId}" +
            $"/collection-routes/{routeId}" +
            $"?tenantId={tenantId}",
            null);

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);
    }

    private async Task<Guid> CreateClientAsync(
        Guid tenantId,
        Guid routeId,
        string firstName,
        string lastName)
    {
        var suffix = Guid.NewGuid().ToString("N");

        var request = new
        {
            tenantId,
            firstName,
            lastName,
            phone = $"809{Random.Shared.Next(1000000, 9999999)}",
            secondaryPhone = (string?)null,
            identificationType = "Cedula",
            identification = $"ID-{suffix}",
            socialNumber = (string?)null,
            address = "Integration Test Address",
            latitude = 19.45m,
            longitude = -70.69m,
            notes = "Agenda integration test",
            collectionRouteId = routeId
        };

        var response = await _client.PostAsJsonAsync(
            "/api/clients",
            request);

        response.EnsureSuccessStatusCode();

        var json =
            await response.Content
                .ReadFromJsonAsync<JsonElement>();

        return json.GetProperty("id").GetGuid();
    }

    private async Task SetManualOrderAsync(
        Guid tenantId,
        Guid routeId,
        IReadOnlyCollection<Guid> clientIds)
    {
        var request = new
        {
            clientIds
        };

        var response = await _client.PutAsJsonAsync(
            $"/api/collection-routes/{routeId}/clients/order" +
            $"?tenantId={tenantId}",
            request);

        response.EnsureSuccessStatusCode();
    }
}