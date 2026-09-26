using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Sanes.Application.CollectionRouteSchedules.DTOs;
using Sanes.Domain.Enums;
using Sanes.Web.CollectionRouteSchedules;

namespace Sanes.Web.Tests;

public class CollectionRouteSchedulesWebServiceTests
{
    [Fact]
    public async Task GetAllAsync_Default_UsesExpectedEndpoint()
    {
        var apiClient =
            new TestSanesApiClient();

        var routeId =
            Guid.NewGuid();

        apiClient.EnqueueResponse(
            JsonResponse(
                new List<CollectionRouteScheduleResponse>()));

        var service =
            new CollectionRouteSchedulesWebService(
                apiClient);

        await service.GetAllAsync(
            routeId);

        AssertRequest(
            Assert.Single(
                apiClient.Requests),
            HttpMethod.Get,
            $"api/collection-route-schedules" +
            $"?collectionRouteId={routeId:D}");
    }

    [Fact]
    public async Task GetAllAsync_IncludeInactive_UsesExpectedEndpoint()
    {
        var apiClient =
            new TestSanesApiClient();

        var routeId =
            Guid.NewGuid();

        apiClient.EnqueueResponse(
            JsonResponse(
                new List<CollectionRouteScheduleResponse>()));

        var service =
            new CollectionRouteSchedulesWebService(
                apiClient);

        await service.GetAllAsync(
            routeId,
            includeInactive: true);

        AssertRequest(
            Assert.Single(
                apiClient.Requests),
            HttpMethod.Get,
            $"api/collection-route-schedules" +
            $"?collectionRouteId={routeId:D}" +
            "&includeInactive=true");
    }

    [Fact]
    public async Task CreateAsync_SendsExpectedRequest()
    {
        var apiClient =
            new TestSanesApiClient();

        var routeId =
            Guid.NewGuid();

        var scheduleId =
            Guid.NewGuid();

        apiClient.EnqueueResponse(
            JsonResponse(
                new CollectionRouteScheduleResponse
                {
                    Id = scheduleId,
                    CollectionRouteId = routeId,
                    DayOfWeek =
                        CollectionWeekDay.Monday,
                    StartTime =
                        new TimeOnly(8, 0),
                    EndTime =
                        new TimeOnly(13, 0),
                    IsActive = true
                },
                HttpStatusCode.Created));

        var service =
            new CollectionRouteSchedulesWebService(
                apiClient);

        var result =
            await service.CreateAsync(
                new CreateCollectionRouteScheduleRequest
                {
                    CollectionRouteId =
                        routeId,
                    DayOfWeek =
                        CollectionWeekDay.Monday,
                    StartTime =
                        new TimeOnly(8, 0),
                    EndTime =
                        new TimeOnly(13, 0)
                });

        Assert.Equal(
            scheduleId,
            result.Id);

        var recorded =
            Assert.Single(
                apiClient.Requests);

        AssertRequest(
            recorded,
            HttpMethod.Post,
            "api/collection-route-schedules");

        Assert.NotNull(
            recorded.Body);

        using var document =
            JsonDocument.Parse(
                recorded.Body!);

        var root =
            document.RootElement;

        Assert.Equal(
            routeId,
            root.GetProperty(
                    "collectionRouteId")
                .GetGuid());

        Assert.Equal(
            (int)CollectionWeekDay.Monday,
            root.GetProperty(
                    "dayOfWeek")
                .GetInt32());

        Assert.False(
            root.TryGetProperty(
                "tenantId",
                out _));
    }

    [Fact]
    public async Task CreateAsync_BadRequest_ThrowsApiMessage()
    {
        var apiClient =
            new TestSanesApiClient();

        apiClient.EnqueueResponse(
            JsonResponse(
                new
                {
                    message =
                        "Este día de cobro ya está configurado para la ruta."
                },
                HttpStatusCode.BadRequest));

        var service =
            new CollectionRouteSchedulesWebService(
                apiClient);

        var exception =
            await Assert.ThrowsAsync<
                ArgumentException>(
                () =>
                    service.CreateAsync(
                        new CreateCollectionRouteScheduleRequest
                        {
                            CollectionRouteId =
                                Guid.NewGuid(),
                            DayOfWeek =
                                CollectionWeekDay.Monday
                        }));

        Assert.Contains(
            "ya está configurado",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateAsync_SendsExpectedRequest()
    {
        var apiClient =
            new TestSanesApiClient();

        var routeId =
            Guid.NewGuid();

        var scheduleId =
            Guid.NewGuid();

        apiClient.EnqueueResponse(
            JsonResponse(
                new CollectionRouteScheduleResponse
                {
                    Id = scheduleId,
                    CollectionRouteId =
                        routeId,
                    DayOfWeek =
                        CollectionWeekDay.Tuesday,
                    IsActive = true
                }));

        var service =
            new CollectionRouteSchedulesWebService(
                apiClient);

        var result =
            await service.UpdateAsync(
                scheduleId,
                routeId,
                new UpdateCollectionRouteScheduleRequest
                {
                    DayOfWeek =
                        CollectionWeekDay.Tuesday
                });

        Assert.NotNull(result);

        var recorded =
            Assert.Single(
                apiClient.Requests);

        AssertRequest(
            recorded,
            HttpMethod.Put,
            $"api/collection-route-schedules/{scheduleId:D}" +
            $"?collectionRouteId={routeId:D}");

        Assert.NotNull(
            recorded.Body);

        using var document =
            JsonDocument.Parse(
                recorded.Body!);

        Assert.False(
            document.RootElement
                .TryGetProperty(
                    "tenantId",
                    out _));
    }

    [Fact]
    public async Task UpdateAsync_NotFound_ReturnsNull()
    {
        var apiClient =
            new TestSanesApiClient();

        apiClient.EnqueueResponse(
            new HttpResponseMessage(
                HttpStatusCode.NotFound));

        var service =
            new CollectionRouteSchedulesWebService(
                apiClient);

        var result =
            await service.UpdateAsync(
                Guid.NewGuid(),
                Guid.NewGuid(),
                new UpdateCollectionRouteScheduleRequest
                {
                    DayOfWeek =
                        CollectionWeekDay.Monday
                });

        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_UsesExpectedEndpoint()
    {
        var apiClient =
            new TestSanesApiClient();

        var routeId =
            Guid.NewGuid();

        var scheduleId =
            Guid.NewGuid();

        apiClient.EnqueueResponse(
            new HttpResponseMessage(
                HttpStatusCode.NoContent));

        var service =
            new CollectionRouteSchedulesWebService(
                apiClient);

        var result =
            await service.DeleteAsync(
                scheduleId,
                routeId);

        Assert.True(result);

        AssertRequest(
            Assert.Single(
                apiClient.Requests),
            HttpMethod.Delete,
            $"api/collection-route-schedules/{scheduleId:D}" +
            $"?collectionRouteId={routeId:D}");
    }

    [Fact]
    public async Task DeleteAsync_NotFound_ReturnsFalse()
    {
        var apiClient =
            new TestSanesApiClient();

        apiClient.EnqueueResponse(
            new HttpResponseMessage(
                HttpStatusCode.NotFound));

        var service =
            new CollectionRouteSchedulesWebService(
                apiClient);

        var result =
            await service.DeleteAsync(
                Guid.NewGuid(),
                Guid.NewGuid());

        Assert.False(result);
    }

    [Fact]
    public async Task ReactivateAsync_UsesExpectedEndpoint()
    {
        var apiClient =
            new TestSanesApiClient();

        var routeId =
            Guid.NewGuid();

        var scheduleId =
            Guid.NewGuid();

        apiClient.EnqueueResponse(
            JsonResponse(
                new CollectionRouteScheduleResponse
                {
                    Id = scheduleId,
                    CollectionRouteId =
                        routeId,
                    DayOfWeek =
                        CollectionWeekDay.Monday,
                    IsActive = true
                }));

        var service =
            new CollectionRouteSchedulesWebService(
                apiClient);

        var result =
            await service.ReactivateAsync(
                scheduleId,
                routeId);

        Assert.NotNull(result);
        Assert.True(
            result.IsActive);

        AssertRequest(
            Assert.Single(
                apiClient.Requests),
            HttpMethod.Patch,
            $"api/collection-route-schedules/{scheduleId:D}/reactivate" +
            $"?collectionRouteId={routeId:D}");
    }

    [Fact]
    public async Task ReactivateAsync_NotFound_ReturnsNull()
    {
        var apiClient =
            new TestSanesApiClient();

        apiClient.EnqueueResponse(
            new HttpResponseMessage(
                HttpStatusCode.NotFound));

        var service =
            new CollectionRouteSchedulesWebService(
                apiClient);

        var result =
            await service.ReactivateAsync(
                Guid.NewGuid(),
                Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public void ToCreateRequest_Converts12HourFormatToTimeOnly()
    {
        var routeId =
            Guid.NewGuid();

        var model =
            new CollectionRouteScheduleFormModel
            {
                DayOfWeek =
                    CollectionWeekDay.Wednesday,
                StartTime =
                    "06:42 PM",
                EndTime =
                    "07:15 PM"
            };

        var request =
            model.ToCreateRequest(
                routeId);

        Assert.Equal(
            routeId,
            request.CollectionRouteId);

        Assert.Equal(
            new TimeOnly(18, 42),
            request.StartTime);

        Assert.Equal(
            new TimeOnly(19, 15),
            request.EndTime);
    }
    private static void AssertRequest(
        TestSanesApiClient.RecordedRequest request,
        HttpMethod method,
        string uri)
    {
        Assert.Equal(
            method,
            request.Method);

        Assert.Equal(
            uri,
            request.Uri);

        Assert.DoesNotContain(
            "tenantId",
            request.Uri,
            StringComparison.OrdinalIgnoreCase);
    }

    private static HttpResponseMessage JsonResponse<T>(
        T value,
        HttpStatusCode statusCode =
            HttpStatusCode.OK)
    {
        return new HttpResponseMessage(
            statusCode)
        {
            Content =
                JsonContent.Create(
                    value)
        };
    }
}

public class CollectionRouteScheduleFormModelTests
{
    [Fact]
    public void Validate_WithoutTimes_IsValid()
    {
        var model =
            new CollectionRouteScheduleFormModel
            {
                DayOfWeek =
                    CollectionWeekDay.Monday
            };

        var results =
            Validate(model);

        Assert.Empty(results);
    }

    [Fact]
    public void Validate_OnlyOneTime_IsInvalid()
    {
        var model =
            new CollectionRouteScheduleFormModel
            {
                DayOfWeek =
                    CollectionWeekDay.Monday,
                StartTime =
                    "08:00"
            };

        var results =
            Validate(model);

        Assert.Contains(
            results,
            x =>
                x.ErrorMessage is not null &&
                x.ErrorMessage.Contains(
                    "deben proporcionarse juntas",
                    StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_EndBeforeStart_IsInvalid()
    {
        var model =
            new CollectionRouteScheduleFormModel
            {
                DayOfWeek =
                    CollectionWeekDay.Monday,
                StartTime =
                    "13:00",
                EndTime =
                    "08:00"
            };

        var results =
            Validate(model);

        Assert.Contains(
            results,
            x =>
                x.ErrorMessage is not null &&
                x.ErrorMessage.Contains(
                    "posterior",
                    StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ToCreateRequest_ConvertsTimesToTimeOnly()
    {
        var routeId =
            Guid.NewGuid();

        var model =
            new CollectionRouteScheduleFormModel
            {
                DayOfWeek =
                    CollectionWeekDay.Wednesday,
                StartTime =
                    "08:30",
                EndTime =
                    "14:45"
            };

        var request =
            model.ToCreateRequest(
                routeId);

        Assert.Equal(
            routeId,
            request.CollectionRouteId);

        Assert.Equal(
            CollectionWeekDay.Wednesday,
            request.DayOfWeek);

        Assert.Equal(
            new TimeOnly(8, 30),
            request.StartTime);

        Assert.Equal(
            new TimeOnly(14, 45),
            request.EndTime);
    }

    [Fact]
    public void Validate_InvalidDay_IsInvalid()
    {
        var model =
            new CollectionRouteScheduleFormModel
            {
                DayOfWeek =
                    (CollectionWeekDay)99
            };

        var results =
            Validate(model);

        Assert.Contains(
            results,
            x =>
                x.MemberNames.Contains(
                    nameof(
                        CollectionRouteScheduleFormModel.DayOfWeek)));
    }

    private static List<ValidationResult> Validate(
        CollectionRouteScheduleFormModel model)
    {
        var results =
            new List<ValidationResult>();

        Validator.TryValidateObject(
            model,
            new ValidationContext(
                model),
            results,
            validateAllProperties: true);

        return results;
    }
}