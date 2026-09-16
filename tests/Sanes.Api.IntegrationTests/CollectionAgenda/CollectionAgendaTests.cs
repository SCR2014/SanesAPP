using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Sanes.Api.IntegrationTests.Helpers;
using Sanes.Application.CollectionAgenda.DTOs;
using Sanes.Application.CollectionRouteSchedules.DTOs;
using Sanes.Domain.Enums;

namespace Sanes.Api.IntegrationTests.CollectionAgenda;

public class CollectionAgendaTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public CollectionAgendaTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAgenda_ForScheduledDay_ReturnsRoute()
    {
        var context = await CreateContextAsync();

        var routeId =
            await CreateRouteAsync(
                context.Client);

        await CreateScheduleAsync(
            context.Client,
            routeId,
            CollectionWeekDay.Monday);

        var response =
            await GetAgendaAsync(
                context.Client,
                new DateOnly(2026, 9, 14));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var agenda =
            await response.Content
                .ReadFromJsonAsync<
                    CollectionAgendaResponse>();

        Assert.NotNull(agenda);

        Assert.Equal(
            CollectionWeekDay.Monday,
            agenda.DayOfWeek);

        Assert.Equal(
            1,
            agenda.RoutesCount);

        Assert.Single(
            agenda.Routes);

        Assert.Equal(
            routeId,
            agenda.Routes[0]
                .CollectionRouteId);
    }

    [Fact]
    public async Task GetAgenda_ForUnscheduledDay_ReturnsEmptyAgenda()
    {
        var context = await CreateContextAsync();

        var routeId =
            await CreateRouteAsync(
                context.Client);

        await CreateScheduleAsync(
            context.Client,
            routeId,
            CollectionWeekDay.Monday);

        var response =
            await GetAgendaAsync(
                context.Client,
                new DateOnly(2026, 9, 15));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var agenda =
            await response.Content
                .ReadFromJsonAsync<
                    CollectionAgendaResponse>();

        Assert.NotNull(agenda);

        Assert.Equal(
            CollectionWeekDay.Tuesday,
            agenda.DayOfWeek);

        Assert.Equal(
            0,
            agenda.RoutesCount);

        Assert.Empty(
            agenda.Routes);
    }

    [Fact]
    public async Task GetAgenda_ReturnsScheduleTime()
    {
        var context = await CreateContextAsync();

        var routeId =
            await CreateRouteAsync(
                context.Client);

        await CreateScheduleAsync(
            context.Client,
            routeId,
            CollectionWeekDay.Monday,
            new TimeOnly(8, 0),
            new TimeOnly(13, 0));

        var response =
            await GetAgendaAsync(
                context.Client,
                new DateOnly(2026, 9, 14));

        var agenda =
            await response.Content
                .ReadFromJsonAsync<
                    CollectionAgendaResponse>();

        Assert.NotNull(agenda);

        var route =
            Assert.Single(
                agenda.Routes);

        Assert.Equal(
            new TimeOnly(8, 0),
            route.StartTime);

        Assert.Equal(
            new TimeOnly(13, 0),
            route.EndTime);
    }

    [Fact]
    public async Task GetAgenda_IncludesAssignedCollector()
    {
        var context = await CreateContextAsync();

        var routeId =
            await CreateRouteAsync(
                context.Client);

        var collectorId =
            await CreateCollectorAsync(
                context.Client);

        await AssignRouteAsync(
            context.Client,
            collectorId,
            routeId);

        await CreateScheduleAsync(
            context.Client,
            routeId,
            CollectionWeekDay.Monday);

        var response =
            await GetAgendaAsync(
                context.Client,
                new DateOnly(2026, 9, 14));

        var agenda =
            await response.Content
                .ReadFromJsonAsync<
                    CollectionAgendaResponse>();

        Assert.NotNull(agenda);

        var route =
            Assert.Single(
                agenda.Routes);

        var collector =
            Assert.Single(
                route.Collectors);

        Assert.Equal(
            collectorId,
            collector.AppUserId);

        Assert.Equal(
            1,
            agenda.CollectorsCount);
    }

    [Fact]
    public async Task GetAgenda_WithoutCollectorStillReturnsScheduledRoute()
    {
        var context = await CreateContextAsync();

        var routeId =
            await CreateRouteAsync(
                context.Client);

        await CreateScheduleAsync(
            context.Client,
            routeId,
            CollectionWeekDay.Monday);

        var response =
            await GetAgendaAsync(
                context.Client,
                new DateOnly(2026, 9, 14));

        var agenda =
            await response.Content
                .ReadFromJsonAsync<
                    CollectionAgendaResponse>();

        Assert.NotNull(agenda);

        var route =
            Assert.Single(
                agenda.Routes);

        Assert.Empty(
            route.Collectors);

        Assert.Equal(
            1,
            agenda.RoutesCount);

        Assert.Equal(
            0,
            agenda.CollectorsCount);
    }

    [Fact]
    public async Task GetAgenda_FilterByCollector_ReturnsOnlyAssignedRoutes()
    {
        var context = await CreateContextAsync();

        var route1 =
            await CreateRouteAsync(
                context.Client);

        var route2 =
            await CreateRouteAsync(
                context.Client);

        var collector1 =
            await CreateCollectorAsync(
                context.Client);

        var collector2 =
            await CreateCollectorAsync(
                context.Client);

        await AssignRouteAsync(
            context.Client,
            collector1,
            route1);

        await AssignRouteAsync(
            context.Client,
            collector2,
            route2);

        await CreateScheduleAsync(
            context.Client,
            route1,
            CollectionWeekDay.Monday);

        await CreateScheduleAsync(
            context.Client,
            route2,
            CollectionWeekDay.Monday);

        var response =
            await GetAgendaAsync(
                context.Client,
                new DateOnly(2026, 9, 14),
                collector1);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var agenda =
            await response.Content
                .ReadFromJsonAsync<
                    CollectionAgendaResponse>();

        Assert.NotNull(agenda);

        Assert.Equal(
            1,
            agenda.RoutesCount);

        var route =
            Assert.Single(
                agenda.Routes);

        Assert.Equal(
            route1,
            route.CollectionRouteId);

        Assert.Single(
            route.Collectors);

        Assert.Equal(
            collector1,
            route.Collectors[0].AppUserId);
    }

    [Fact]
    public async Task GetAgenda_FilterByCollector_WithNoAssignedRoute_ReturnsEmpty()
    {
        var context = await CreateContextAsync();

        var routeId =
            await CreateRouteAsync(
                context.Client);

        var collectorId =
            await CreateCollectorAsync(
                context.Client);

        await CreateScheduleAsync(
            context.Client,
            routeId,
            CollectionWeekDay.Monday);

        var response =
            await GetAgendaAsync(
                context.Client,
                new DateOnly(2026, 9, 14),
                collectorId);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var agenda =
            await response.Content
                .ReadFromJsonAsync<
                    CollectionAgendaResponse>();

        Assert.NotNull(agenda);

        Assert.Equal(
            0,
            agenda.RoutesCount);

        Assert.Empty(
            agenda.Routes);
    }

    [Fact]
    public async Task GetAgenda_FilterByAdministrator_ReturnsBadRequest()
    {
        var context = await CreateContextAsync();

        var response =
            await GetAgendaAsync(
                context.Client,
                new DateOnly(2026, 9, 14),
                context.AdministratorId);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task GetAgenda_FilterByUserFromAnotherTenant_ReturnsBadRequest()
    {
        var tenant1 = await CreateContextAsync();
        var tenant2 = await CreateContextAsync();

        var collectorId =
            await CreateCollectorAsync(
                tenant1.Client);

        var response =
            await GetAgendaAsync(
                tenant2.Client,
                new DateOnly(2026, 9, 14),
                collectorId);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task GetAgenda_DoesNotReturnRouteFromAnotherTenant()
    {
        var tenant1 = await CreateContextAsync();
        var tenant2 = await CreateContextAsync();

        var route1 =
            await CreateRouteAsync(
                tenant1.Client);

        var route2 =
            await CreateRouteAsync(
                tenant2.Client);

        await CreateScheduleAsync(
            tenant1.Client,
            route1,
            CollectionWeekDay.Monday);

        await CreateScheduleAsync(
            tenant2.Client,
            route2,
            CollectionWeekDay.Monday);

        var response =
            await GetAgendaAsync(
                tenant1.Client,
                new DateOnly(2026, 9, 14));

        var agenda =
            await response.Content
                .ReadFromJsonAsync<
                    CollectionAgendaResponse>();

        Assert.NotNull(agenda);

        Assert.Equal(
            1,
            agenda.RoutesCount);

        Assert.DoesNotContain(
            agenda.Routes,
            x =>
                x.CollectionRouteId ==
                route2);
    }

    [Fact]
    public async Task GetAgenda_IncludesClientsAssignedToRoute()
    {
        var context = await CreateContextAsync();

        var routeId =
            await CreateRouteAsync(
                context.Client);

        var clientId =
            await CreateClientAsync(
                context.Client,
                routeId,
                "Agenda",
                "Client");

        await CreateScheduleAsync(
            context.Client,
            routeId,
            CollectionWeekDay.Monday);

        var response =
            await GetAgendaAsync(
                context.Client,
                new DateOnly(2026, 9, 14));

        var agenda =
            await response.Content
                .ReadFromJsonAsync<
                    CollectionAgendaResponse>();

        Assert.NotNull(agenda);

        var route =
            Assert.Single(
                agenda.Routes);

        var client =
            Assert.Single(
                route.Clients);

        Assert.Equal(
            clientId,
            client.ClientId);

        Assert.Equal(
            "Agenda",
            client.FirstName);

        Assert.Equal(
            "Client",
            client.LastName);

        Assert.Equal(
            1,
            agenda.ClientsCount);
    }

    [Fact]
    public async Task GetAgenda_DoesNotIncludeClientFromAnotherTenant()
    {
        var tenant1 = await CreateContextAsync();
        var tenant2 = await CreateContextAsync();

        var route1 =
            await CreateRouteAsync(
                tenant1.Client);

        var route2 =
            await CreateRouteAsync(
                tenant2.Client);

        var client2 =
            await CreateClientAsync(
                tenant2.Client,
                route2,
                "Other",
                "Tenant");

        await CreateScheduleAsync(
            tenant1.Client,
            route1,
            CollectionWeekDay.Monday);

        await CreateScheduleAsync(
            tenant2.Client,
            route2,
            CollectionWeekDay.Monday);

        var response =
            await GetAgendaAsync(
                tenant1.Client,
                new DateOnly(2026, 9, 14));

        var agenda =
            await response.Content
                .ReadFromJsonAsync<
                    CollectionAgendaResponse>();

        Assert.NotNull(agenda);

        Assert.DoesNotContain(
            agenda.Routes
                .SelectMany(
                    x => x.Clients),
            x =>
                x.ClientId ==
                client2);
    }

    [Fact]
    public async Task GetAgenda_OrdersClientsByCollectionRouteOrder()
    {
        var context = await CreateContextAsync();

        var routeId =
            await CreateRouteAsync(
                context.Client);

        var client1 =
            await CreateClientAsync(
                context.Client,
                routeId,
                "First",
                "Client");

        var client2 =
            await CreateClientAsync(
                context.Client,
                routeId,
                "Second",
                "Client");

        var client3 =
            await CreateClientAsync(
                context.Client,
                routeId,
                "Third",
                "Client");

        await SetManualOrderAsync(
            context.Client,
            routeId,
            new[]
            {
                client3,
                client1,
                client2
            });

        await CreateScheduleAsync(
            context.Client,
            routeId,
            CollectionWeekDay.Monday);

        var response =
            await GetAgendaAsync(
                context.Client,
                new DateOnly(2026, 9, 14));

        var agenda =
            await response.Content
                .ReadFromJsonAsync<
                    CollectionAgendaResponse>();

        Assert.NotNull(agenda);

        var route =
            Assert.Single(
                agenda.Routes);

        Assert.Equal(
            3,
            route.Clients.Count);

        Assert.Equal(
            client3,
            route.Clients[0].ClientId);

        Assert.Equal(
            client1,
            route.Clients[1].ClientId);

        Assert.Equal(
            client2,
            route.Clients[2].ClientId);

        Assert.Equal(
            1,
            route.Clients[0]
                .CollectionRouteOrder);

        Assert.Equal(
            2,
            route.Clients[1]
                .CollectionRouteOrder);

        Assert.Equal(
            3,
            route.Clients[2]
                .CollectionRouteOrder);
    }

    [Fact]
    public async Task GetAgenda_SoftDeletedScheduleIsNotReturned()
    {
        var context = await CreateContextAsync();

        var routeId =
            await CreateRouteAsync(
                context.Client);

        var schedule =
            await CreateScheduleAsync(
                context.Client,
                routeId,
                CollectionWeekDay.Monday);

        var deleteResponse =
            await context.Client.DeleteAsync(
                $"/api/collection-route-schedules/{schedule.Id}" +
                $"?collectionRouteId={routeId}");

        Assert.Equal(
            HttpStatusCode.NoContent,
            deleteResponse.StatusCode);

        var response =
            await GetAgendaAsync(
                context.Client,
                new DateOnly(2026, 9, 14));

        var agenda =
            await response.Content
                .ReadFromJsonAsync<
                    CollectionAgendaResponse>();

        Assert.NotNull(agenda);

        Assert.Equal(
            0,
            agenda.RoutesCount);
    }

    [Fact]
    public async Task GetAgenda_InactiveCollectorIsNotReturned()
    {
        var context = await CreateContextAsync();

        var routeId =
            await CreateRouteAsync(
                context.Client);

        var collectorId =
            await CreateCollectorAsync(
                context.Client);

        await AssignRouteAsync(
            context.Client,
            collectorId,
            routeId);

        await CreateScheduleAsync(
            context.Client,
            routeId,
            CollectionWeekDay.Monday);

        var deleteUserResponse =
            await context.Client.DeleteAsync(
                $"/api/app-users/{collectorId}");

        Assert.Equal(
            HttpStatusCode.NoContent,
            deleteUserResponse.StatusCode);

        var response =
            await GetAgendaAsync(
                context.Client,
                new DateOnly(2026, 9, 14));

        var agenda =
            await response.Content
                .ReadFromJsonAsync<
                    CollectionAgendaResponse>();

        Assert.NotNull(agenda);

        var route =
            Assert.Single(
                agenda.Routes);

        Assert.Empty(
            route.Collectors);

        Assert.Equal(
            0,
            agenda.CollectorsCount);
    }

    [Fact]
    public async Task GetAgenda_ReturnsDistinctCollectorCount()
    {
        var context = await CreateContextAsync();

        var route1 =
            await CreateRouteAsync(
                context.Client);

        var route2 =
            await CreateRouteAsync(
                context.Client);

        var collectorId =
            await CreateCollectorAsync(
                context.Client);

        await AssignRouteAsync(
            context.Client,
            collectorId,
            route1);

        await AssignRouteAsync(
            context.Client,
            collectorId,
            route2);

        await CreateScheduleAsync(
            context.Client,
            route1,
            CollectionWeekDay.Monday);

        await CreateScheduleAsync(
            context.Client,
            route2,
            CollectionWeekDay.Monday);

        var response =
            await GetAgendaAsync(
                context.Client,
                new DateOnly(2026, 9, 14));

        var agenda =
            await response.Content
                .ReadFromJsonAsync<
                    CollectionAgendaResponse>();

        Assert.NotNull(agenda);

        Assert.Equal(
            2,
            agenda.RoutesCount);

        Assert.Equal(
            1,
            agenda.CollectorsCount);
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

    private static async Task<HttpResponseMessage>
        GetAgendaAsync(
            HttpClient client,
            DateOnly date,
            Guid? appUserId = null)
    {
        var url =
            $"/api/collection-agenda" +
            $"?date={date:yyyy-MM-dd}";

        if (appUserId.HasValue)
        {
            url +=
                $"&appUserId={appUserId.Value}";
        }

        return await client.GetAsync(
            url);
    }

    private static async Task<Guid>
        CreateRouteAsync(
            HttpClient client)
    {
        var suffix =
            Guid.NewGuid()
                .ToString("N");

        var request = new
        {
            name =
                $"Agenda Route {suffix}",
            description =
                "Agenda integration test route",
            orderMode = 1
        };

        var response =
            await client.PostAsJsonAsync(
                "/api/collection-routes",
                request);

        response.EnsureSuccessStatusCode();

        var json =
            await response.Content
                .ReadFromJsonAsync<
                    JsonElement>();

        return json
            .GetProperty("id")
            .GetGuid();
    }

    private static async Task<
        CollectionRouteScheduleResponse>
        CreateScheduleAsync(
            HttpClient client,
            Guid routeId,
            CollectionWeekDay day,
            TimeOnly? startTime = null,
            TimeOnly? endTime = null)
    {
        var request =
            new CreateCollectionRouteScheduleRequest
            {
                CollectionRouteId = routeId,
                DayOfWeek = day,
                StartTime = startTime,
                EndTime = endTime
            };

        var response =
            await client.PostAsJsonAsync(
                "/api/collection-route-schedules",
                request);

        response.EnsureSuccessStatusCode();

        var schedule =
            await response.Content
                .ReadFromJsonAsync<
                    CollectionRouteScheduleResponse>();

        Assert.NotNull(schedule);

        return schedule;
    }

    private static async Task<Guid>
        CreateCollectorAsync(
            HttpClient client)
    {
        return await CreateAppUserAsync(
            client,
            AppUserRole.Collector);
    }

    private static async Task<Guid>
        CreateAppUserAsync(
            HttpClient client,
            AppUserRole role)
    {
        var suffix =
            Guid.NewGuid()
                .ToString("N");

        var request = new
        {
            name =
                $"Agenda User {suffix}",
            username =
                $"agenda-{suffix}",
            password =
                TestAuthenticationHelper
                    .DefaultPassword,
            email =
                $"agenda-user-{suffix}@example.com",
            phone =
                "8095551234",
            role = (int)role
        };

        var response =
            await client.PostAsJsonAsync(
                "/api/app-users",
                request);

        response.EnsureSuccessStatusCode();

        var json =
            await response.Content
                .ReadFromJsonAsync<
                    JsonElement>();

        return json
            .GetProperty("id")
            .GetGuid();
    }

    private static async Task
        AssignRouteAsync(
            HttpClient client,
            Guid collectorId,
            Guid routeId)
    {
        var response =
            await client.PostAsync(
                $"/api/app-users/{collectorId}" +
                $"/collection-routes/{routeId}",
                null);

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);
    }

    private static async Task<Guid>
        CreateClientAsync(
            HttpClient client,
            Guid routeId,
            string firstName,
            string lastName)
    {
        var suffix =
            Guid.NewGuid()
                .ToString("N");

        var request = new
        {
            firstName,
            lastName,
            phone =
                $"809{Random.Shared.Next(
                    1000000,
                    9999999)}",
            secondaryPhone =
                (string?)null,
            identificationType =
                "Cedula",
            identification =
                $"ID-{suffix}",
            socialNumber =
                (string?)null,
            address =
                "Integration Test Address",
            latitude =
                19.45m,
            longitude =
                -70.69m,
            notes =
                "Agenda integration test",
            collectionRouteId =
                routeId
        };

        var response =
            await client.PostAsJsonAsync(
                "/api/clients",
                request);

        response.EnsureSuccessStatusCode();

        var json =
            await response.Content
                .ReadFromJsonAsync<
                    JsonElement>();

        return json
            .GetProperty("id")
            .GetGuid();
    }

    private static async Task
        SetManualOrderAsync(
            HttpClient client,
            Guid routeId,
            IReadOnlyCollection<Guid> clientIds)
    {
        var request = new
        {
            clientIds
        };

        var response =
            await client.PutAsJsonAsync(
                $"/api/collection-routes/{routeId}" +
                "/clients/order",
                request);

        response.EnsureSuccessStatusCode();
    }
}