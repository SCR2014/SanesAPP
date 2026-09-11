using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Sanes.Application.CollectionRouteSchedules.DTOs;
using Sanes.Domain.Enums;

namespace Sanes.Api.IntegrationTests.CollectionRouteSchedules;

public class CollectionRouteSchedulesTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CollectionRouteSchedulesTests(
        CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Create_WithValidData_ReturnsCreated()
    {
        var tenantId = await CreateTenantAsync();
        var routeId = await CreateRouteAsync(tenantId);

        var request = new CreateCollectionRouteScheduleRequest
        {
            TenantId = tenantId,
            CollectionRouteId = routeId,
            DayOfWeek = CollectionWeekDay.Monday,
            StartTime = new TimeOnly(8, 0),
            EndTime = new TimeOnly(13, 0)
        };

        var response = await _client.PostAsJsonAsync(
            "/api/collection-route-schedules",
            request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var schedule =
            await response.Content
                .ReadFromJsonAsync<CollectionRouteScheduleResponse>();

        Assert.NotNull(schedule);
        Assert.Equal(routeId, schedule.CollectionRouteId);
        Assert.Equal(CollectionWeekDay.Monday, schedule.DayOfWeek);
        Assert.Equal(new TimeOnly(8, 0), schedule.StartTime);
        Assert.Equal(new TimeOnly(13, 0), schedule.EndTime);
        Assert.True(schedule.IsActive);
    }

    [Fact]
    public async Task Create_WithoutTimes_ReturnsCreated()
    {
        var tenantId = await CreateTenantAsync();
        var routeId = await CreateRouteAsync(tenantId);

        var request = new CreateCollectionRouteScheduleRequest
        {
            TenantId = tenantId,
            CollectionRouteId = routeId,
            DayOfWeek = CollectionWeekDay.Tuesday
        };

        var response = await _client.PostAsJsonAsync(
            "/api/collection-route-schedules",
            request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var schedule =
            await response.Content
                .ReadFromJsonAsync<CollectionRouteScheduleResponse>();

        Assert.NotNull(schedule);
        Assert.Null(schedule.StartTime);
        Assert.Null(schedule.EndTime);
    }

    [Fact]
    public async Task Create_WithOnlyStartTime_ReturnsBadRequest()
    {
        var tenantId = await CreateTenantAsync();
        var routeId = await CreateRouteAsync(tenantId);

        var request = new CreateCollectionRouteScheduleRequest
        {
            TenantId = tenantId,
            CollectionRouteId = routeId,
            DayOfWeek = CollectionWeekDay.Wednesday,
            StartTime = new TimeOnly(8, 0)
        };

        var response = await _client.PostAsJsonAsync(
            "/api/collection-route-schedules",
            request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithOnlyEndTime_ReturnsBadRequest()
    {
        var tenantId = await CreateTenantAsync();
        var routeId = await CreateRouteAsync(tenantId);

        var request = new CreateCollectionRouteScheduleRequest
        {
            TenantId = tenantId,
            CollectionRouteId = routeId,
            DayOfWeek = CollectionWeekDay.Wednesday,
            EndTime = new TimeOnly(13, 0)
        };

        var response = await _client.PostAsJsonAsync(
            "/api/collection-route-schedules",
            request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithEndTimeBeforeStartTime_ReturnsBadRequest()
    {
        var tenantId = await CreateTenantAsync();
        var routeId = await CreateRouteAsync(tenantId);

        var request = new CreateCollectionRouteScheduleRequest
        {
            TenantId = tenantId,
            CollectionRouteId = routeId,
            DayOfWeek = CollectionWeekDay.Thursday,
            StartTime = new TimeOnly(14, 0),
            EndTime = new TimeOnly(8, 0)
        };

        var response = await _client.PostAsJsonAsync(
            "/api/collection-route-schedules",
            request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithEqualStartAndEndTime_ReturnsBadRequest()
    {
        var tenantId = await CreateTenantAsync();
        var routeId = await CreateRouteAsync(tenantId);

        var request = new CreateCollectionRouteScheduleRequest
        {
            TenantId = tenantId,
            CollectionRouteId = routeId,
            DayOfWeek = CollectionWeekDay.Friday,
            StartTime = new TimeOnly(8, 0),
            EndTime = new TimeOnly(8, 0)
        };

        var response = await _client.PostAsJsonAsync(
            "/api/collection-route-schedules",
            request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithInvalidDay_ReturnsBadRequest()
    {
        var tenantId = await CreateTenantAsync();
        var routeId = await CreateRouteAsync(tenantId);

        var request = new CreateCollectionRouteScheduleRequest
        {
            TenantId = tenantId,
            CollectionRouteId = routeId,
            DayOfWeek = (CollectionWeekDay)99
        };

        var response = await _client.PostAsJsonAsync(
            "/api/collection-route-schedules",
            request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_DuplicateDay_ReturnsBadRequest()
    {
        var tenantId = await CreateTenantAsync();
        var routeId = await CreateRouteAsync(tenantId);

        await CreateScheduleAsync(
            tenantId,
            routeId,
            CollectionWeekDay.Monday);

        var request = new CreateCollectionRouteScheduleRequest
        {
            TenantId = tenantId,
            CollectionRouteId = routeId,
            DayOfWeek = CollectionWeekDay.Monday
        };

        var response = await _client.PostAsJsonAsync(
            "/api/collection-route-schedules",
            request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_ReturnsOnlyActiveSchedulesOrderedByDay()
    {
        var tenantId = await CreateTenantAsync();
        var routeId = await CreateRouteAsync(tenantId);

        await CreateScheduleAsync(
            tenantId,
            routeId,
            CollectionWeekDay.Friday);

        await CreateScheduleAsync(
            tenantId,
            routeId,
            CollectionWeekDay.Monday);

        await CreateScheduleAsync(
            tenantId,
            routeId,
            CollectionWeekDay.Wednesday);

        var response = await _client.GetAsync(
            $"/api/collection-route-schedules" +
            $"?tenantId={tenantId}" +
            $"&collectionRouteId={routeId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var schedules =
            await response.Content
                .ReadFromJsonAsync<List<CollectionRouteScheduleResponse>>();

        Assert.NotNull(schedules);
        Assert.Equal(3, schedules.Count);

        Assert.Equal(
            CollectionWeekDay.Monday,
            schedules[0].DayOfWeek);

        Assert.Equal(
            CollectionWeekDay.Wednesday,
            schedules[1].DayOfWeek);

        Assert.Equal(
            CollectionWeekDay.Friday,
            schedules[2].DayOfWeek);
    }

    [Fact]
    public async Task GetById_WithCorrectTenant_ReturnsSchedule()
    {
        var tenantId = await CreateTenantAsync();
        var routeId = await CreateRouteAsync(tenantId);

        var schedule = await CreateScheduleAsync(
            tenantId,
            routeId,
            CollectionWeekDay.Monday);

        var response = await _client.GetAsync(
            $"/api/collection-route-schedules/{schedule.Id}" +
            $"?tenantId={tenantId}" +
            $"&collectionRouteId={routeId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetById_WithDifferentTenant_ReturnsNotFound()
    {
        var tenant1 = await CreateTenantAsync();
        var tenant2 = await CreateTenantAsync();

        var routeId = await CreateRouteAsync(tenant1);

        var schedule = await CreateScheduleAsync(
            tenant1,
            routeId,
            CollectionWeekDay.Monday);

        var response = await _client.GetAsync(
            $"/api/collection-route-schedules/{schedule.Id}" +
            $"?tenantId={tenant2}" +
            $"&collectionRouteId={routeId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_ChangesDayAndTime()
    {
        var tenantId = await CreateTenantAsync();
        var routeId = await CreateRouteAsync(tenantId);

        var schedule = await CreateScheduleAsync(
            tenantId,
            routeId,
            CollectionWeekDay.Monday);

        var request = new UpdateCollectionRouteScheduleRequest
        {
            DayOfWeek = CollectionWeekDay.Tuesday,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(15, 0)
        };

        var response = await _client.PutAsJsonAsync(
            $"/api/collection-route-schedules/{schedule.Id}" +
            $"?tenantId={tenantId}" +
            $"&collectionRouteId={routeId}",
            request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updated =
            await response.Content
                .ReadFromJsonAsync<CollectionRouteScheduleResponse>();

        Assert.NotNull(updated);
        Assert.Equal(CollectionWeekDay.Tuesday, updated.DayOfWeek);
        Assert.Equal(new TimeOnly(9, 0), updated.StartTime);
        Assert.Equal(new TimeOnly(15, 0), updated.EndTime);
    }

    [Fact]
    public async Task Update_ToExistingDay_ReturnsBadRequest()
    {
        var tenantId = await CreateTenantAsync();
        var routeId = await CreateRouteAsync(tenantId);

        var monday = await CreateScheduleAsync(
            tenantId,
            routeId,
            CollectionWeekDay.Monday);

        await CreateScheduleAsync(
            tenantId,
            routeId,
            CollectionWeekDay.Tuesday);

        var request = new UpdateCollectionRouteScheduleRequest
        {
            DayOfWeek = CollectionWeekDay.Tuesday
        };

        var response = await _client.PutAsJsonAsync(
            $"/api/collection-route-schedules/{monday.Id}" +
            $"?tenantId={tenantId}" +
            $"&collectionRouteId={routeId}",
            request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Delete_SoftDeletesSchedule()
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

        var getResponse = await _client.GetAsync(
            $"/api/collection-route-schedules/{schedule.Id}" +
            $"?tenantId={tenantId}" +
            $"&collectionRouteId={routeId}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            getResponse.StatusCode);
    }

    [Fact]
    public async Task Create_DayPreviouslySoftDeleted_ReturnsBadRequest()
    {
        var tenantId = await CreateTenantAsync();
        var routeId = await CreateRouteAsync(tenantId);

        var schedule = await CreateScheduleAsync(
            tenantId,
            routeId,
            CollectionWeekDay.Monday);

        await _client.DeleteAsync(
            $"/api/collection-route-schedules/{schedule.Id}" +
            $"?tenantId={tenantId}" +
            $"&collectionRouteId={routeId}");

        var request = new CreateCollectionRouteScheduleRequest
        {
            TenantId = tenantId,
            CollectionRouteId = routeId,
            DayOfWeek = CollectionWeekDay.Monday
        };

        var response = await _client.PostAsJsonAsync(
            "/api/collection-route-schedules",
            request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Reactivate_RestoresSoftDeletedSchedule()
    {
        var tenantId = await CreateTenantAsync();
        var routeId = await CreateRouteAsync(tenantId);

        var schedule = await CreateScheduleAsync(
            tenantId,
            routeId,
            CollectionWeekDay.Monday);

        await _client.DeleteAsync(
            $"/api/collection-route-schedules/{schedule.Id}" +
            $"?tenantId={tenantId}" +
            $"&collectionRouteId={routeId}");

        var response = await _client.PatchAsync(
            $"/api/collection-route-schedules/{schedule.Id}/reactivate" +
            $"?tenantId={tenantId}" +
            $"&collectionRouteId={routeId}",
            null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var restored =
            await response.Content
                .ReadFromJsonAsync<CollectionRouteScheduleResponse>();

        Assert.NotNull(restored);
        Assert.True(restored.IsActive);
    }

    [Fact]
    public async Task Delete_WithDifferentTenant_ReturnsNotFound()
    {
        var tenant1 = await CreateTenantAsync();
        var tenant2 = await CreateTenantAsync();

        var routeId = await CreateRouteAsync(tenant1);

        var schedule = await CreateScheduleAsync(
            tenant1,
            routeId,
            CollectionWeekDay.Monday);

        var response = await _client.DeleteAsync(
            $"/api/collection-route-schedules/{schedule.Id}" +
            $"?tenantId={tenant2}" +
            $"&collectionRouteId={routeId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<Guid> CreateTenantAsync()
    {
        var suffix = Guid.NewGuid().ToString("N");

        var request = new
        {
            name = $"Schedule Tenant {suffix}",
            legalName = $"Schedule Tenant {suffix}",
            phone = "8095551234",
            email = $"schedule-{suffix}@example.com",
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
            name = $"Schedule Route {suffix}",
            description = "Integration test route",
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
            CollectionWeekDay day)
    {
        var request = new CreateCollectionRouteScheduleRequest
        {
            TenantId = tenantId,
            CollectionRouteId = routeId,
            DayOfWeek = day
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
}