using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Sanes.Api.IntegrationTests.Helpers;
using Sanes.Application.CollectionRouteSchedules.DTOs;
using Sanes.Domain.Enums;

namespace Sanes.Api.IntegrationTests.CollectionRouteSchedules;

public class CollectionRouteSchedulesTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public CollectionRouteSchedulesTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Create_WithValidData_ReturnsCreated()
    {
        var context = await CreateContextAsync();
        var routeId = await CreateRouteAsync(context.Client);

        var request =
            new CreateCollectionRouteScheduleRequest
            {
                CollectionRouteId = routeId,
                DayOfWeek = CollectionWeekDay.Monday,
                StartTime = new TimeOnly(8, 0),
                EndTime = new TimeOnly(13, 0)
            };

        var response =
            await context.Client.PostAsJsonAsync(
                "/api/collection-route-schedules",
                request);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var schedule =
            await response.Content
                .ReadFromJsonAsync<
                    CollectionRouteScheduleResponse>();

        Assert.NotNull(schedule);

        Assert.Equal(
            routeId,
            schedule.CollectionRouteId);

        Assert.Equal(
            CollectionWeekDay.Monday,
            schedule.DayOfWeek);

        Assert.Equal(
            new TimeOnly(8, 0),
            schedule.StartTime);

        Assert.Equal(
            new TimeOnly(13, 0),
            schedule.EndTime);

        Assert.True(schedule.IsActive);
    }

    [Fact]
    public async Task Create_WithoutTimes_ReturnsCreated()
    {
        var context = await CreateContextAsync();
        var routeId = await CreateRouteAsync(context.Client);

        var request =
            new CreateCollectionRouteScheduleRequest
            {
                CollectionRouteId = routeId,
                DayOfWeek = CollectionWeekDay.Tuesday
            };

        var response =
            await context.Client.PostAsJsonAsync(
                "/api/collection-route-schedules",
                request);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var schedule =
            await response.Content
                .ReadFromJsonAsync<
                    CollectionRouteScheduleResponse>();

        Assert.NotNull(schedule);
        Assert.Null(schedule.StartTime);
        Assert.Null(schedule.EndTime);
    }

    [Fact]
    public async Task Create_WithOnlyStartTime_ReturnsBadRequest()
    {
        var context = await CreateContextAsync();
        var routeId = await CreateRouteAsync(context.Client);

        var request =
            new CreateCollectionRouteScheduleRequest
            {
                CollectionRouteId = routeId,
                DayOfWeek = CollectionWeekDay.Wednesday,
                StartTime = new TimeOnly(8, 0)
            };

        var response =
            await context.Client.PostAsJsonAsync(
                "/api/collection-route-schedules",
                request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task Create_WithOnlyEndTime_ReturnsBadRequest()
    {
        var context = await CreateContextAsync();
        var routeId = await CreateRouteAsync(context.Client);

        var request =
            new CreateCollectionRouteScheduleRequest
            {
                CollectionRouteId = routeId,
                DayOfWeek = CollectionWeekDay.Wednesday,
                EndTime = new TimeOnly(13, 0)
            };

        var response =
            await context.Client.PostAsJsonAsync(
                "/api/collection-route-schedules",
                request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task Create_WithEndTimeBeforeStartTime_ReturnsBadRequest()
    {
        var context = await CreateContextAsync();
        var routeId = await CreateRouteAsync(context.Client);

        var request =
            new CreateCollectionRouteScheduleRequest
            {
                CollectionRouteId = routeId,
                DayOfWeek = CollectionWeekDay.Thursday,
                StartTime = new TimeOnly(14, 0),
                EndTime = new TimeOnly(8, 0)
            };

        var response =
            await context.Client.PostAsJsonAsync(
                "/api/collection-route-schedules",
                request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task Create_WithEqualStartAndEndTime_ReturnsBadRequest()
    {
        var context = await CreateContextAsync();
        var routeId = await CreateRouteAsync(context.Client);

        var request =
            new CreateCollectionRouteScheduleRequest
            {
                CollectionRouteId = routeId,
                DayOfWeek = CollectionWeekDay.Friday,
                StartTime = new TimeOnly(8, 0),
                EndTime = new TimeOnly(8, 0)
            };

        var response =
            await context.Client.PostAsJsonAsync(
                "/api/collection-route-schedules",
                request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task Create_WithInvalidDay_ReturnsBadRequest()
    {
        var context = await CreateContextAsync();
        var routeId = await CreateRouteAsync(context.Client);

        var request =
            new CreateCollectionRouteScheduleRequest
            {
                CollectionRouteId = routeId,
                DayOfWeek = (CollectionWeekDay)99
            };

        var response =
            await context.Client.PostAsJsonAsync(
                "/api/collection-route-schedules",
                request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task Create_DuplicateDay_ReturnsBadRequest()
    {
        var context = await CreateContextAsync();
        var routeId = await CreateRouteAsync(context.Client);

        await CreateScheduleAsync(
            context.Client,
            routeId,
            CollectionWeekDay.Monday);

        var request =
            new CreateCollectionRouteScheduleRequest
            {
                CollectionRouteId = routeId,
                DayOfWeek = CollectionWeekDay.Monday
            };

        var response =
            await context.Client.PostAsJsonAsync(
                "/api/collection-route-schedules",
                request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task GetAll_ReturnsOnlyActiveSchedulesOrderedByDay()
    {
        var context = await CreateContextAsync();
        var routeId = await CreateRouteAsync(context.Client);

        await CreateScheduleAsync(
            context.Client,
            routeId,
            CollectionWeekDay.Friday);

        await CreateScheduleAsync(
            context.Client,
            routeId,
            CollectionWeekDay.Monday);

        await CreateScheduleAsync(
            context.Client,
            routeId,
            CollectionWeekDay.Wednesday);

        var response =
            await context.Client.GetAsync(
                $"/api/collection-route-schedules" +
                $"?collectionRouteId={routeId}");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var schedules =
            await response.Content
                .ReadFromJsonAsync<
                    List<CollectionRouteScheduleResponse>>();

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
        var context = await CreateContextAsync();
        var routeId = await CreateRouteAsync(context.Client);

        var schedule =
            await CreateScheduleAsync(
                context.Client,
                routeId,
                CollectionWeekDay.Monday);

        var response =
            await context.Client.GetAsync(
                $"/api/collection-route-schedules/{schedule.Id}" +
                $"?collectionRouteId={routeId}");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);
    }

    [Fact]
    public async Task GetById_WithDifferentTenant_ReturnsNotFound()
    {
        var tenant1 = await CreateContextAsync();
        var tenant2 = await CreateContextAsync();

        var routeId =
            await CreateRouteAsync(
                tenant1.Client);

        var schedule =
            await CreateScheduleAsync(
                tenant1.Client,
                routeId,
                CollectionWeekDay.Monday);

        var response =
            await tenant2.Client.GetAsync(
                $"/api/collection-route-schedules/{schedule.Id}" +
                $"?collectionRouteId={routeId}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task Update_ChangesDayAndTime()
    {
        var context = await CreateContextAsync();
        var routeId = await CreateRouteAsync(context.Client);

        var schedule =
            await CreateScheduleAsync(
                context.Client,
                routeId,
                CollectionWeekDay.Monday);

        var request =
            new UpdateCollectionRouteScheduleRequest
            {
                DayOfWeek = CollectionWeekDay.Tuesday,
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(15, 0)
            };

        var response =
            await context.Client.PutAsJsonAsync(
                $"/api/collection-route-schedules/{schedule.Id}" +
                $"?collectionRouteId={routeId}",
                request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var updated =
            await response.Content
                .ReadFromJsonAsync<
                    CollectionRouteScheduleResponse>();

        Assert.NotNull(updated);

        Assert.Equal(
            CollectionWeekDay.Tuesday,
            updated.DayOfWeek);

        Assert.Equal(
            new TimeOnly(9, 0),
            updated.StartTime);

        Assert.Equal(
            new TimeOnly(15, 0),
            updated.EndTime);
    }

    [Fact]
    public async Task Update_ToExistingDay_ReturnsBadRequest()
    {
        var context = await CreateContextAsync();
        var routeId = await CreateRouteAsync(context.Client);

        var monday =
            await CreateScheduleAsync(
                context.Client,
                routeId,
                CollectionWeekDay.Monday);

        await CreateScheduleAsync(
            context.Client,
            routeId,
            CollectionWeekDay.Tuesday);

        var request =
            new UpdateCollectionRouteScheduleRequest
            {
                DayOfWeek = CollectionWeekDay.Tuesday
            };

        var response =
            await context.Client.PutAsJsonAsync(
                $"/api/collection-route-schedules/{monday.Id}" +
                $"?collectionRouteId={routeId}",
                request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task Delete_SoftDeletesSchedule()
    {
        var context = await CreateContextAsync();
        var routeId = await CreateRouteAsync(context.Client);

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

        var getResponse =
            await context.Client.GetAsync(
                $"/api/collection-route-schedules/{schedule.Id}" +
                $"?collectionRouteId={routeId}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            getResponse.StatusCode);
    }

    [Fact]
    public async Task Create_DayPreviouslySoftDeleted_ReturnsBadRequest()
    {
        var context = await CreateContextAsync();
        var routeId = await CreateRouteAsync(context.Client);

        var schedule =
            await CreateScheduleAsync(
                context.Client,
                routeId,
                CollectionWeekDay.Monday);

        await context.Client.DeleteAsync(
            $"/api/collection-route-schedules/{schedule.Id}" +
            $"?collectionRouteId={routeId}");

        var request =
            new CreateCollectionRouteScheduleRequest
            {
                CollectionRouteId = routeId,
                DayOfWeek = CollectionWeekDay.Monday
            };

        var response =
            await context.Client.PostAsJsonAsync(
                "/api/collection-route-schedules",
                request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task Reactivate_RestoresSoftDeletedSchedule()
    {
        var context = await CreateContextAsync();
        var routeId = await CreateRouteAsync(context.Client);

        var schedule =
            await CreateScheduleAsync(
                context.Client,
                routeId,
                CollectionWeekDay.Monday);

        await context.Client.DeleteAsync(
            $"/api/collection-route-schedules/{schedule.Id}" +
            $"?collectionRouteId={routeId}");

        var response =
            await context.Client.PatchAsync(
                $"/api/collection-route-schedules/{schedule.Id}/reactivate" +
                $"?collectionRouteId={routeId}",
                null);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var restored =
            await response.Content
                .ReadFromJsonAsync<
                    CollectionRouteScheduleResponse>();

        Assert.NotNull(restored);
        Assert.True(restored.IsActive);
    }

    [Fact]
    public async Task Delete_WithDifferentTenant_ReturnsNotFound()
    {
        var tenant1 = await CreateContextAsync();
        var tenant2 = await CreateContextAsync();

        var routeId =
            await CreateRouteAsync(
                tenant1.Client);

        var schedule =
            await CreateScheduleAsync(
                tenant1.Client,
                routeId,
                CollectionWeekDay.Monday);

        var response =
            await tenant2.Client.DeleteAsync(
                $"/api/collection-route-schedules/{schedule.Id}" +
                $"?collectionRouteId={routeId}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
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

    private static async Task<Guid>
        CreateRouteAsync(
            HttpClient client)
    {
        var suffix =
            Guid.NewGuid().ToString("N");

        var response =
            await client.PostAsJsonAsync(
                "/api/collection-routes",
                new
                {
                    name =
                        $"Schedule Route {suffix}",
                    description =
                        "Integration test route",
                    orderMode = 1
                });

        response.EnsureSuccessStatusCode();

        var json =
            await response.Content
                .ReadFromJsonAsync<JsonElement>();

        return json
            .GetProperty("id")
            .GetGuid();
    }

    private static async Task<
        CollectionRouteScheduleResponse>
        CreateScheduleAsync(
            HttpClient client,
            Guid routeId,
            CollectionWeekDay day)
    {
        var request =
            new CreateCollectionRouteScheduleRequest
            {
                CollectionRouteId = routeId,
                DayOfWeek = day
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
}