using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Sanes.Application.AppUsers.DTOs;
using Sanes.Domain.Enums;
using Sanes.Web.AppUsers;

namespace Sanes.Web.Tests;

public class AppUsersWebServiceTests
{
    [Fact]
    public async Task GetAllAsync_Default_UsesBaseEndpoint()
    {
        var apiClient =
            new TestSanesApiClient();

        apiClient.EnqueueResponse(
            JsonResponse(
                new List<AppUserResponse>
                {
                    CreateUserResponse()
                }));

        var service =
            CreateService(
                apiClient);

        var result =
            await service.GetAllAsync();

        Assert.Single(
            result);

        AssertRequest(
            Assert.Single(
                apiClient.Requests),
            HttpMethod.Get,
            "api/app-users");
    }

    [Fact]
    public async Task GetAllAsync_WithRoleAndInactive_BuildsExpectedUrl()
    {
        var apiClient =
            new TestSanesApiClient();

        apiClient.EnqueueResponse(
            JsonResponse(
                new List<AppUserResponse>()));

        var service =
            CreateService(
                apiClient);

        await service.GetAllAsync(
            AppUserRole.Collector,
            includeInactive: true);

        AssertRequest(
            Assert.Single(
                apiClient.Requests),
            HttpMethod.Get,
            "api/app-users?role=2&includeInactive=true");
    }

    [Fact]
    public async Task GetAllAsync_WhenBadRequest_TranslatesMessage()
    {
        var apiClient =
            new TestSanesApiClient();

        apiClient.EnqueueResponse(
            ApiError(
                "Invalid app user role."));

        var service =
            CreateService(
                apiClient);

        var exception =
            await Assert.ThrowsAsync<
                ArgumentException>(
                () =>
                    service.GetAllAsync(
                        (AppUserRole)99));

        Assert.Equal(
            "El rol seleccionado no es válido.",
            exception.Message);
    }

    [Fact]
    public async Task GetByIdAsync_UsesExpectedEndpoint()
    {
        var apiClient =
            new TestSanesApiClient();

        var appUser =
            CreateUserResponse();

        apiClient.EnqueueResponse(
            JsonResponse(
                appUser));

        var service =
            CreateService(
                apiClient);

        var result =
            await service.GetByIdAsync(
                appUser.Id);

        Assert.NotNull(
            result);

        Assert.Equal(
            appUser.Id,
            result.Id);

        AssertRequest(
            Assert.Single(
                apiClient.Requests),
            HttpMethod.Get,
            $"api/app-users/{appUser.Id:D}");
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNull()
    {
        var apiClient =
            new TestSanesApiClient();

        apiClient.EnqueueResponse(
            new HttpResponseMessage(
                HttpStatusCode.NotFound));

        var service =
            CreateService(
                apiClient);

        var result =
            await service.GetByIdAsync(
                Guid.NewGuid());

        Assert.Null(
            result);
    }

    [Fact]
    public async Task GetByIdAsync_WithEmptyId_ThrowsBeforeRequest()
    {
        var apiClient =
            new TestSanesApiClient();

        var service =
            CreateService(
                apiClient);

        var exception =
            await Assert.ThrowsAsync<
                ArgumentException>(
                () =>
                    service.GetByIdAsync(
                        Guid.Empty));

        Assert.Contains(
            "usuario",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        Assert.Empty(
            apiClient.Requests);
    }

    [Fact]
    public async Task CreateAsync_SendsExpectedRequest()
    {
        var apiClient =
            new TestSanesApiClient();

        var appUserId =
            Guid.NewGuid();

        apiClient.EnqueueResponse(
            JsonResponse(
                new AppUserResponse
                {
                    Id =
                        appUserId,
                    Name =
                        "Cobrador Web",
                    Username =
                        "collectorweb",
                    Email =
                        "collector@sanes.test",
                    Phone =
                        "8095550001",
                    Role =
                        AppUserRole.Collector,
                    IsActive =
                        true
                },
                HttpStatusCode.Created));

        var service =
            CreateService(
                apiClient);

        var result =
            await service.CreateAsync(
                new CreateAppUserRequest
                {
                    Name =
                        "Cobrador Web",
                    Username =
                        "collectorweb",
                    Password =
                        "Password#2026",
                    Email =
                        "collector@sanes.test",
                    Phone =
                        "8095550001",
                    Role =
                        AppUserRole.Collector
                });

        Assert.Equal(
            appUserId,
            result.Id);

        var recorded =
            Assert.Single(
                apiClient.Requests);

        AssertRequest(
            recorded,
            HttpMethod.Post,
            "api/app-users");

        Assert.NotNull(
            recorded.Body);

        using var document =
            JsonDocument.Parse(
                recorded.Body!);

        var root =
            document.RootElement;

        Assert.Equal(
            "Cobrador Web",
            root.GetProperty(
                    "name")
                .GetString());

        Assert.Equal(
            "collectorweb",
            root.GetProperty(
                    "username")
                .GetString());

        Assert.Equal(
            "Password#2026",
            root.GetProperty(
                    "password")
                .GetString());

        Assert.Equal(
            (int)AppUserRole.Collector,
            root.GetProperty(
                    "role")
                .GetInt32());

        Assert.False(
            root.TryGetProperty(
                "tenantId",
                out _));
    }

    [Fact]
    public async Task CreateAsync_WhenDuplicateUsername_TranslatesMessage()
    {
        var apiClient =
            new TestSanesApiClient();

        apiClient.EnqueueResponse(
            ApiError(
                "Username already exists for this tenant."));

        var service =
            CreateService(
                apiClient);

        var exception =
            await Assert.ThrowsAsync<
                ArgumentException>(
                () =>
                    service.CreateAsync(
                        new CreateAppUserRequest
                        {
                            Name =
                                "Cobrador",
                            Username =
                                "collector",
                            Password =
                                "Password#2026",
                            Role =
                                AppUserRole.Collector
                        }));

        Assert.Equal(
            "Ya existe un usuario con ese nombre de usuario.",
            exception.Message);
    }

    [Fact]
    public async Task UpdateAsync_SendsExpectedRequest()
    {
        var apiClient =
            new TestSanesApiClient();

        var appUserId =
            Guid.NewGuid();

        apiClient.EnqueueResponse(
            JsonResponse(
                new AppUserResponse
                {
                    Id =
                        appUserId,
                    Name =
                        "Cobrador Actualizado",
                    Username =
                        "collectorupdated",
                    Role =
                        AppUserRole.Collector,
                    IsActive =
                        true
                }));

        var service =
            CreateService(
                apiClient);

        var result =
            await service.UpdateAsync(
                appUserId,
                new UpdateAppUserRequest
                {
                    Name =
                        "Cobrador Actualizado",
                    Username =
                        "collectorupdated",
                    Email =
                        "updated@sanes.test",
                    Phone =
                        "8095559999",
                    Role =
                        AppUserRole.Collector
                });

        Assert.NotNull(
            result);

        var recorded =
            Assert.Single(
                apiClient.Requests);

        AssertRequest(
            recorded,
            HttpMethod.Put,
            $"api/app-users/{appUserId:D}");

        Assert.NotNull(
            recorded.Body);

        using var document =
            JsonDocument.Parse(
                recorded.Body!);

        var root =
            document.RootElement;

        Assert.Equal(
            "Cobrador Actualizado",
            root.GetProperty(
                    "name")
                .GetString());

        Assert.Equal(
            "collectorupdated",
            root.GetProperty(
                    "username")
                .GetString());

        Assert.False(
            root.TryGetProperty(
                "tenantId",
                out _));

        Assert.False(
            root.TryGetProperty(
                "password",
                out _));
    }

    [Fact]
    public async Task UpdateAsync_WhenNotFound_ReturnsNull()
    {
        var apiClient =
            new TestSanesApiClient();

        apiClient.EnqueueResponse(
            new HttpResponseMessage(
                HttpStatusCode.NotFound));

        var service =
            CreateService(
                apiClient);

        var result =
            await service.UpdateAsync(
                Guid.NewGuid(),
                new UpdateAppUserRequest
                {
                    Name =
                        "Usuario",
                    Username =
                        "usuario",
                    Role =
                        AppUserRole.Collector
                });

        Assert.Null(
            result);
    }

    [Fact]
    public async Task UpdateAsync_CollectorWithRoutes_TranslatesMessage()
    {
        var apiClient =
            new TestSanesApiClient();

        apiClient.EnqueueResponse(
            ApiError(
                "A collector with assigned collection routes cannot change role. Unassign the routes first."));

        var service =
            CreateService(
                apiClient);

        var exception =
            await Assert.ThrowsAsync<
                InvalidOperationException>(
                () =>
                    service.UpdateAsync(
                        Guid.NewGuid(),
                        new UpdateAppUserRequest
                        {
                            Name =
                                "Cobrador",
                            Username =
                                "collector",
                            Role =
                                AppUserRole.Administrator
                        }));

        Assert.Equal(
            "El cobrador tiene rutas asignadas. Debe desasignarlas antes de cambiar su rol.",
            exception.Message);
    }

    [Fact]
    public async Task DeleteAsync_UsesExpectedEndpoint()
    {
        var apiClient =
            new TestSanesApiClient();

        var appUserId =
            Guid.NewGuid();

        apiClient.EnqueueResponse(
            new HttpResponseMessage(
                HttpStatusCode.NoContent));

        var service =
            CreateService(
                apiClient);

        var result =
            await service.DeleteAsync(
                appUserId);

        Assert.True(
            result);

        AssertRequest(
            Assert.Single(
                apiClient.Requests),
            HttpMethod.Delete,
            $"api/app-users/{appUserId:D}");
    }

    [Fact]
    public async Task DeleteAsync_WhenNotFound_ReturnsFalse()
    {
        var apiClient =
            new TestSanesApiClient();

        apiClient.EnqueueResponse(
            new HttpResponseMessage(
                HttpStatusCode.NotFound));

        var service =
            CreateService(
                apiClient);

        var result =
            await service.DeleteAsync(
                Guid.NewGuid());

        Assert.False(
            result);
    }

    [Fact]
    public async Task DeleteAsync_LastAdministrator_TranslatesMessage()
    {
        var apiClient =
            new TestSanesApiClient();

        apiClient.EnqueueResponse(
            ApiError(
                "The last active administrator cannot be deactivated."));

        var service =
            CreateService(
                apiClient);

        var exception =
            await Assert.ThrowsAsync<
                InvalidOperationException>(
                () =>
                    service.DeleteAsync(
                        Guid.NewGuid()));

        Assert.Equal(
            "El último administrador activo no puede ser desactivado.",
            exception.Message);
    }

    [Fact]
    public async Task ReactivateAsync_UsesExpectedEndpoint()
    {
        var apiClient =
            new TestSanesApiClient();

        var appUser =
            CreateUserResponse();

        apiClient.EnqueueResponse(
            JsonResponse(
                appUser));

        var service =
            CreateService(
                apiClient);

        var result =
            await service.ReactivateAsync(
                appUser.Id);

        Assert.NotNull(
            result);

        AssertRequest(
            Assert.Single(
                apiClient.Requests),
            HttpMethod.Patch,
            $"api/app-users/{appUser.Id:D}/reactivate");
    }

    [Fact]
    public async Task ReactivateAsync_WhenNotFound_ReturnsNull()
    {
        var apiClient =
            new TestSanesApiClient();

        apiClient.EnqueueResponse(
            new HttpResponseMessage(
                HttpStatusCode.NotFound));

        var service =
            CreateService(
                apiClient);

        var result =
            await service.ReactivateAsync(
                Guid.NewGuid());

        Assert.Null(
            result);
    }

    [Fact]
    public async Task GetCollectionRoutesAsync_ReturnsAssignments()
    {
        var apiClient =
            new TestSanesApiClient();

        var appUserId =
            Guid.NewGuid();

        var routeId =
            Guid.NewGuid();

        apiClient.EnqueueResponse(
            JsonResponse(
                new List<
                    AppUserCollectionRouteResponse>
                {
                    new()
                    {
                        CollectionRouteId =
                            routeId,
                        CollectionRouteName =
                            "Ruta Santiago Centro",
                        AssignedAt =
                            DateTime.UtcNow
                    }
                }));

        var service =
            CreateService(
                apiClient);

        var result =
            await service
                .GetCollectionRoutesAsync(
                    appUserId);

        Assert.NotNull(
            result);

        var assignment =
            Assert.Single(
                result);

        Assert.Equal(
            routeId,
            assignment.CollectionRouteId);

        AssertRequest(
            Assert.Single(
                apiClient.Requests),
            HttpMethod.Get,
            $"api/app-users/{appUserId:D}/collection-routes");
    }

    [Fact]
    public async Task GetCollectionRoutesAsync_WhenNotFound_ReturnsNull()
    {
        var apiClient =
            new TestSanesApiClient();

        apiClient.EnqueueResponse(
            new HttpResponseMessage(
                HttpStatusCode.NotFound));

        var service =
            CreateService(
                apiClient);

        var result =
            await service
                .GetCollectionRoutesAsync(
                    Guid.NewGuid());

        Assert.Null(
            result);
    }

    [Fact]
    public async Task AssignCollectionRouteAsync_UsesExpectedEndpoint()
    {
        var apiClient =
            new TestSanesApiClient();

        var appUserId =
            Guid.NewGuid();

        var routeId =
            Guid.NewGuid();

        apiClient.EnqueueResponse(
            new HttpResponseMessage(
                HttpStatusCode.NoContent));

        var service =
            CreateService(
                apiClient);

        var result =
            await service
                .AssignCollectionRouteAsync(
                    appUserId,
                    routeId);

        Assert.True(
            result);

        AssertRequest(
            Assert.Single(
                apiClient.Requests),
            HttpMethod.Post,
            $"api/app-users/{appUserId:D}/collection-routes/{routeId:D}");
    }

    [Fact]
    public async Task AssignCollectionRouteAsync_WhenAlreadyAssigned_TranslatesMessage()
    {
        var apiClient =
            new TestSanesApiClient();

        apiClient.EnqueueResponse(
            ApiError(
                "Collection route is already assigned to this user."));

        var service =
            CreateService(
                apiClient);

        var exception =
            await Assert.ThrowsAsync<
                InvalidOperationException>(
                () =>
                    service
                        .AssignCollectionRouteAsync(
                            Guid.NewGuid(),
                            Guid.NewGuid()));

        Assert.Equal(
            "La ruta ya está asignada a este cobrador.",
            exception.Message);
    }

    [Fact]
    public async Task AssignCollectionRouteAsync_WithEmptyRouteId_ThrowsBeforeRequest()
    {
        var apiClient =
            new TestSanesApiClient();

        var service =
            CreateService(
                apiClient);

        var exception =
            await Assert.ThrowsAsync<
                ArgumentException>(
                () =>
                    service
                        .AssignCollectionRouteAsync(
                            Guid.NewGuid(),
                            Guid.Empty));

        Assert.Contains(
            "ruta",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        Assert.Empty(
            apiClient.Requests);
    }

    [Fact]
    public async Task UnassignCollectionRouteAsync_UsesExpectedEndpoint()
    {
        var apiClient =
            new TestSanesApiClient();

        var appUserId =
            Guid.NewGuid();

        var routeId =
            Guid.NewGuid();

        apiClient.EnqueueResponse(
            new HttpResponseMessage(
                HttpStatusCode.NoContent));

        var service =
            CreateService(
                apiClient);

        var result =
            await service
                .UnassignCollectionRouteAsync(
                    appUserId,
                    routeId);

        Assert.True(
            result);

        AssertRequest(
            Assert.Single(
                apiClient.Requests),
            HttpMethod.Delete,
            $"api/app-users/{appUserId:D}/collection-routes/{routeId:D}");
    }

    [Fact]
    public async Task UnassignCollectionRouteAsync_WhenNotFound_ReturnsFalse()
    {
        var apiClient =
            new TestSanesApiClient();

        apiClient.EnqueueResponse(
            new HttpResponseMessage(
                HttpStatusCode.NotFound));

        var service =
            CreateService(
                apiClient);

        var result =
            await service
                .UnassignCollectionRouteAsync(
                    Guid.NewGuid(),
                    Guid.NewGuid());

        Assert.False(
            result);
    }

    [Fact]
    public async Task SetPasswordAsync_SendsExpectedRequest()
    {
        var apiClient =
            new TestSanesApiClient();

        var appUserId =
            Guid.NewGuid();

        apiClient.EnqueueResponse(
            new HttpResponseMessage(
                HttpStatusCode.NoContent));

        var service =
            CreateService(
                apiClient);

        var result =
            await service.SetPasswordAsync(
                appUserId,
                "Password#2027");

        Assert.True(
            result);

        var recorded =
            Assert.Single(
                apiClient.Requests);

        AssertRequest(
            recorded,
            HttpMethod.Patch,
            $"api/app-users/{appUserId:D}/password");

        Assert.NotNull(
            recorded.Body);

        using var document =
            JsonDocument.Parse(
                recorded.Body!);

        var root =
            document.RootElement;

        Assert.Equal(
            "Password#2027",
            root.GetProperty(
                    "password")
                .GetString());

        Assert.False(
            root.TryGetProperty(
                "tenantId",
                out _));
    }

    [Fact]
    public async Task SetPasswordAsync_InactiveUser_TranslatesMessage()
    {
        var apiClient =
            new TestSanesApiClient();

        apiClient.EnqueueResponse(
            ApiError(
                "Password cannot be changed for an inactive app user."));

        var service =
            CreateService(
                apiClient);

        var exception =
            await Assert.ThrowsAsync<
                InvalidOperationException>(
                () =>
                    service.SetPasswordAsync(
                        Guid.NewGuid(),
                        "Password#2027"));

        Assert.Equal(
            "No se puede cambiar la contraseña de un usuario inactivo.",
            exception.Message);
    }

    [Fact]
    public async Task SetPasswordAsync_WithEmptyPassword_ThrowsBeforeRequest()
    {
        var apiClient =
            new TestSanesApiClient();

        var service =
            CreateService(
                apiClient);

        var exception =
            await Assert.ThrowsAsync<
                ArgumentException>(
                () =>
                    service.SetPasswordAsync(
                        Guid.NewGuid(),
                        " "));

        Assert.Contains(
            "contraseña",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        Assert.Empty(
            apiClient.Requests);
    }

    [Fact]
    public void AppUserFormModel_FromResponse_MapsValues()
    {
        var appUser =
            CreateUserResponse();

        var model =
            AppUserFormModel
                .FromResponse(
                    appUser);

        Assert.Equal(
            appUser.Name,
            model.Name);

        Assert.Equal(
            appUser.Username,
            model.Username);

        Assert.Equal(
            appUser.Email,
            model.Email);

        Assert.Equal(
            appUser.Phone,
            model.Phone);

        Assert.Equal(
            appUser.Role,
            model.Role);
    }

    [Fact]
    public void AppUserFormModel_ToRequests_NormalizesOptionalValues()
    {
        var model =
            new AppUserFormModel
            {
                Name =
                    "  Cobrador Web  ",
                Username =
                    "  CollectorWeb  ",
                Email =
                    "  collector@sanes.test  ",
                Phone =
                    "  8095550001  ",
                Role =
                    AppUserRole.Collector
            };

        var createRequest =
            model.ToCreateRequest(
                "Password#2026");

        var updateRequest =
            model.ToUpdateRequest();

        Assert.Equal(
            "Cobrador Web",
            createRequest.Name);

        Assert.Equal(
            "CollectorWeb",
            createRequest.Username);

        Assert.Equal(
            "collector@sanes.test",
            createRequest.Email);

        Assert.Equal(
            "8095550001",
            createRequest.Phone);

        Assert.Equal(
            "Password#2026",
            createRequest.Password);

        Assert.Equal(
            "Cobrador Web",
            updateRequest.Name);

        Assert.Equal(
            "CollectorWeb",
            updateRequest.Username);

        Assert.Equal(
            AppUserRole.Collector,
            updateRequest.Role);
    }

    [Fact]
    public void AppUserPasswordFormModel_WhenPasswordsDiffer_IsInvalid()
    {
        var model =
            new AppUserPasswordFormModel
            {
                Password =
                    "Password#2026",
                ConfirmPassword =
                    "Password#2027"
            };

        var validationResults =
            new List<ValidationResult>();

        var valid =
            Validator.TryValidateObject(
                model,
                new ValidationContext(
                    model),
                validationResults,
                validateAllProperties: true);

        Assert.False(
            valid);

        Assert.Contains(
            validationResults,
            x =>
                x.ErrorMessage ==
                "Las contraseñas no coinciden.");
    }

    private static AppUsersWebService
        CreateService(
            TestSanesApiClient apiClient)
    {
        return new AppUsersWebService(
            apiClient);
    }

    private static AppUserResponse
        CreateUserResponse()
    {
        return new AppUserResponse
        {
            Id =
                Guid.NewGuid(),

            TenantId =
                Guid.NewGuid(),

            Name =
                "Cobrador Web",

            Username =
                "collectorweb",

            Email =
                "collector@sanes.test",

            Phone =
                "8095550001",

            Role =
                AppUserRole.Collector,

            IsActive =
                true,

            CreatedAt =
                DateTime.UtcNow.AddMinutes(-10),

            UpdatedAt =
                DateTime.UtcNow
        };
    }

    private static void AssertRequest(
        TestSanesApiClient.RecordedRequest request,
        HttpMethod expectedMethod,
        string expectedUri)
    {
        Assert.Equal(
            expectedMethod,
            request.Method);

        Assert.Equal(
            expectedUri,
            request.Uri);

        Assert.False(
            request.Uri.Contains(
                "tenantId",
                StringComparison.OrdinalIgnoreCase));

        if (request.Body is not null)
        {
            Assert.False(
                request.Body.Contains(
                    "tenantId",
                    StringComparison.OrdinalIgnoreCase));
        }
    }

    private static HttpResponseMessage
        JsonResponse<T>(
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

    private static HttpResponseMessage
        ApiError(
            string message,
            HttpStatusCode statusCode =
                HttpStatusCode.BadRequest)
    {
        return new HttpResponseMessage(
            statusCode)
        {
            Content =
                JsonContent.Create(
                    new
                    {
                        message
                    })
        };
    }
}