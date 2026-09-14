using System.Net;
using System.Net.Http.Json;
using Sanes.Api.IntegrationTests.Helpers;
using Sanes.Application.AppUsers.DTOs;
using Sanes.Application.CollectionRoutes.DTOs;
using Sanes.Domain.Enums;

namespace Sanes.Api.IntegrationTests.AppUsers;

public class AppUsersTests :
    IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AppUsersTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ============================================================
    // CREATE
    // ============================================================

    [Fact]
    public async Task Create_Administrator_ReturnsCreated()
    {
        var context =
            await CreateContextAsync();

        var username =
            Unique("admin");

        var response =
            await CreateAppUserAsync(
                context.Client,
                "Administrador Test",
                username,
                AppUserRole.Administrator);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var appUser =
            await response.Content
                .ReadFromJsonAsync<AppUserResponse>();

        Assert.NotNull(appUser);
        Assert.Equal(
            context.TenantId,
            appUser.TenantId);
        Assert.Equal(
            "Administrador Test",
            appUser.Name);
        Assert.Equal(
            username.ToLowerInvariant(),
            appUser.Username);
        Assert.Equal(
            AppUserRole.Administrator,
            appUser.Role);
        Assert.True(appUser.IsActive);
    }

    [Fact]
    public async Task Create_Collector_ReturnsCreated()
    {
        var context =
            await CreateContextAsync();

        var response =
            await CreateAppUserAsync(
                context.Client,
                "Cobrador Test",
                Unique("collector"),
                AppUserRole.Collector);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var appUser =
            await response.Content
                .ReadFromJsonAsync<AppUserResponse>();

        Assert.NotNull(appUser);
        Assert.Equal(
            AppUserRole.Collector,
            appUser.Role);
        Assert.True(appUser.IsActive);
    }

    [Fact]
    public async Task Create_NormalizesUsername()
    {
        var context =
            await CreateContextAsync();

        var rawUsername =
            $"  USER_{Guid.NewGuid():N}  ";

        var response =
            await CreateAppUserAsync(
                context.Client,
                "Usuario Normalizado",
                rawUsername,
                AppUserRole.Collector);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var appUser =
            await response.Content
                .ReadFromJsonAsync<AppUserResponse>();

        Assert.NotNull(appUser);

        Assert.Equal(
            rawUsername
                .Trim()
                .ToLowerInvariant(),
            appUser.Username);
    }

    [Fact]
    public async Task Create_DuplicateUsernameSameTenant_ReturnsBadRequest()
    {
        var context =
            await CreateContextAsync();

        var username =
            Unique("duplicate");

        var firstResponse =
            await CreateAppUserAsync(
                context.Client,
                "Usuario Uno",
                username,
                AppUserRole.Collector);

        Assert.Equal(
            HttpStatusCode.Created,
            firstResponse.StatusCode);

        var secondResponse =
            await CreateAppUserAsync(
                context.Client,
                "Usuario Dos",
                username.ToUpperInvariant(),
                AppUserRole.Collector);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            secondResponse.StatusCode);
    }

    [Fact]
    public async Task Create_SameUsernameDifferentTenant_ReturnsCreated()
    {
        var tenant1 =
            await CreateContextAsync();

        var tenant2 =
            await CreateContextAsync();

        var username =
            Unique("shared");

        var firstResponse =
            await CreateAppUserAsync(
                tenant1.Client,
                "Usuario Tenant 1",
                username,
                AppUserRole.Collector);

        var secondResponse =
            await CreateAppUserAsync(
                tenant2.Client,
                "Usuario Tenant 2",
                username,
                AppUserRole.Collector);

        Assert.Equal(
            HttpStatusCode.Created,
            firstResponse.StatusCode);

        Assert.Equal(
            HttpStatusCode.Created,
            secondResponse.StatusCode);
    }

    [Fact]
    public async Task Create_InvalidEmail_ReturnsBadRequest()
    {
        var context =
            await CreateContextAsync();

        var request =
            new CreateAppUserRequest
            {
                Name =
                    "Usuario Email",
                Username =
                    Unique("email"),
                Password =
                    TestAuthenticationHelper
                        .DefaultPassword,
                Email =
                    "correo-invalido",
                Phone =
                    "8095551234",
                Role =
                    AppUserRole.Collector
            };

        var response =
            await context.Client
                .PostAsJsonAsync(
                    "/api/app-users",
                    request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task Create_InvalidRole_ReturnsBadRequest()
    {
        var context =
            await CreateContextAsync();

        var request = new
        {
            name = "Usuario Role",
            username = Unique("role"),
            password =
                TestAuthenticationHelper
                    .DefaultPassword,
            email = "role@test.com",
            phone = "8095551234",
            role = 99
        };

        var response =
            await context.Client
                .PostAsJsonAsync(
                    "/api/app-users",
                    request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    // ============================================================
    // GET
    // ============================================================

    [Fact]
    public async Task GetAll_ReturnsActiveUsersForTenant()
    {
        var context =
            await CreateContextAsync();

        var username1 =
            Unique("getall1");

        var username2 =
            Unique("getall2");

        await CreateAppUserAndGetAsync(
            context.Client,
            "Usuario Uno",
            username1,
            AppUserRole.Administrator);

        await CreateAppUserAndGetAsync(
            context.Client,
            "Usuario Dos",
            username2,
            AppUserRole.Collector);

        var response =
            await context.Client.GetAsync(
                "/api/app-users");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var users =
            await response.Content
                .ReadFromJsonAsync<
                    List<AppUserResponse>>();

        Assert.NotNull(users);

        Assert.Contains(
            users,
            x =>
                x.Username ==
                username1.ToLowerInvariant());

        Assert.Contains(
            users,
            x =>
                x.Username ==
                username2.ToLowerInvariant());
    }

    [Fact]
    public async Task GetAll_WithCollectorRole_ReturnsOnlyCollectors()
    {
        var context =
            await CreateContextAsync();

        var collector =
            await CreateAppUserAndGetAsync(
                context.Client,
                "Cobrador Filtro",
                Unique("collectorfilter"),
                AppUserRole.Collector);

        await CreateAppUserAndGetAsync(
            context.Client,
            "Administrador Filtro",
            Unique("adminfilter"),
            AppUserRole.Administrator);

        var response =
            await context.Client.GetAsync(
                $"/api/app-users" +
                $"?role={(int)AppUserRole.Collector}");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var users =
            await response.Content
                .ReadFromJsonAsync<
                    List<AppUserResponse>>();

        Assert.NotNull(users);

        Assert.Contains(
            users,
            x => x.Id == collector.Id);

        Assert.All(
            users,
            x => Assert.Equal(
                AppUserRole.Collector,
                x.Role));
    }

    [Fact]
    public async Task GetAll_WithInvalidRole_ReturnsBadRequest()
    {
        var context =
            await CreateContextAsync();

        var response =
            await context.Client.GetAsync(
                "/api/app-users?role=99");

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task GetById_ExistingUser_ReturnsUser()
    {
        var context =
            await CreateContextAsync();

        var created =
            await CreateAppUserAndGetAsync(
                context.Client,
                "Usuario Detalle",
                Unique("detail"),
                AppUserRole.Collector);

        var response =
            await context.Client.GetAsync(
                $"/api/app-users/{created.Id}");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var user =
            await response.Content
                .ReadFromJsonAsync<AppUserResponse>();

        Assert.NotNull(user);
        Assert.Equal(
            created.Id,
            user.Id);
        Assert.Equal(
            context.TenantId,
            user.TenantId);
    }

    [Fact]
    public async Task GetById_UserFromDifferentTenant_ReturnsNotFound()
    {
        var tenant1 =
            await CreateContextAsync();

        var tenant2 =
            await CreateContextAsync();

        var created =
            await CreateAppUserAndGetAsync(
                tenant1.Client,
                "Usuario Tenant Isolation",
                Unique("isolation"),
                AppUserRole.Collector);

        var response =
            await tenant2.Client.GetAsync(
                $"/api/app-users/{created.Id}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    // ============================================================
    // UPDATE
    // ============================================================

    [Fact]
    public async Task Update_ActiveUser_UpdatesValues()
    {
        var context =
            await CreateContextAsync();

        var created =
            await CreateAppUserAndGetAsync(
                context.Client,
                "Usuario Original",
                Unique("update"),
                AppUserRole.Collector);

        var newUsername =
            Unique("updated");

        var request =
            new UpdateAppUserRequest
            {
                Name =
                    "Usuario Actualizado",
                Username =
                    newUsername,
                Email =
                    "actualizado@test.com",
                Phone =
                    "8095559999",
                Role =
                    AppUserRole.Collector
            };

        var response =
            await context.Client.PutAsJsonAsync(
                $"/api/app-users/{created.Id}",
                request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var updated =
            await response.Content
                .ReadFromJsonAsync<AppUserResponse>();

        Assert.NotNull(updated);
        Assert.Equal(
            "Usuario Actualizado",
            updated.Name);
        Assert.Equal(
            newUsername.ToLowerInvariant(),
            updated.Username);
        Assert.Equal(
            "actualizado@test.com",
            updated.Email);
        Assert.Equal(
            "8095559999",
            updated.Phone);
    }

    [Fact]
    public async Task Update_KeepingSameUsername_ReturnsOk()
    {
        var context =
            await CreateContextAsync();

        var created =
            await CreateAppUserAndGetAsync(
                context.Client,
                "Usuario Mismo Username",
                Unique("same"),
                AppUserRole.Collector);

        var request =
            new UpdateAppUserRequest
            {
                Name =
                    "Nombre Actualizado",
                Username =
                    created.Username
                        .ToUpperInvariant(),
                Email =
                    "same@test.com",
                Phone =
                    "8095551111",
                Role =
                    AppUserRole.Collector
            };

        var response =
            await context.Client.PutAsJsonAsync(
                $"/api/app-users/{created.Id}",
                request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);
    }

    [Fact]
    public async Task Update_WithDuplicateUsername_ReturnsBadRequest()
    {
        var context =
            await CreateContextAsync();

        var user1 =
            await CreateAppUserAndGetAsync(
                context.Client,
                "Usuario Uno",
                Unique("dup1"),
                AppUserRole.Collector);

        var user2 =
            await CreateAppUserAndGetAsync(
                context.Client,
                "Usuario Dos",
                Unique("dup2"),
                AppUserRole.Collector);

        var request =
            new UpdateAppUserRequest
            {
                Name = user2.Name,
                Username = user1.Username,
                Email =
                    "duplicate@test.com",
                Phone =
                    "8095552222",
                Role =
                    AppUserRole.Collector
            };

        var response =
            await context.Client.PutAsJsonAsync(
                $"/api/app-users/{user2.Id}",
                request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task Update_UserFromDifferentTenant_ReturnsNotFound()
    {
        var tenant1 =
            await CreateContextAsync();

        var tenant2 =
            await CreateContextAsync();

        var created =
            await CreateAppUserAndGetAsync(
                tenant1.Client,
                "Usuario Update Tenant",
                Unique("updatetenant"),
                AppUserRole.Collector);

        var request =
            new UpdateAppUserRequest
            {
                Name =
                    "Intento Otro Tenant",
                Username =
                    created.Username,
                Email =
                    "tenant@test.com",
                Phone =
                    "8095553333",
                Role =
                    AppUserRole.Collector
            };

        var response =
            await tenant2.Client.PutAsJsonAsync(
                $"/api/app-users/{created.Id}",
                request);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    // ============================================================
    // SOFT DELETE / REACTIVATE
    // ============================================================

    [Fact]
    public async Task Delete_ActiveUser_ReturnsNoContent()
    {
        var context =
            await CreateContextAsync();

        var created =
            await CreateAppUserAndGetAsync(
                context.Client,
                "Usuario Delete",
                Unique("delete"),
                AppUserRole.Collector);

        var response =
            await context.Client.DeleteAsync(
                $"/api/app-users/{created.Id}");

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        var getResponse =
            await context.Client.GetAsync(
                $"/api/app-users/{created.Id}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_UserDisappearsFromActiveList()
    {
        var context =
            await CreateContextAsync();

        var created =
            await CreateAppUserAndGetAsync(
                context.Client,
                "Usuario Lista Delete",
                Unique("listdelete"),
                AppUserRole.Collector);

        var deleteResponse =
            await context.Client.DeleteAsync(
                $"/api/app-users/{created.Id}");

        Assert.Equal(
            HttpStatusCode.NoContent,
            deleteResponse.StatusCode);

        var listResponse =
            await context.Client.GetAsync(
                "/api/app-users");

        var users =
            await listResponse.Content
                .ReadFromJsonAsync<
                    List<AppUserResponse>>();

        Assert.NotNull(users);

        Assert.DoesNotContain(
            users,
            x => x.Id == created.Id);
    }

    [Fact]
    public async Task Update_InactiveUser_ReturnsNotFound()
    {
        var context =
            await CreateContextAsync();

        var created =
            await CreateAppUserAndGetAsync(
                context.Client,
                "Usuario Inactivo",
                Unique("inactiveupdate"),
                AppUserRole.Collector);

        await context.Client.DeleteAsync(
            $"/api/app-users/{created.Id}");

        var request =
            new UpdateAppUserRequest
            {
                Name =
                    "No Debe Actualizar",
                Username =
                    created.Username,
                Email =
                    "inactive@test.com",
                Phone =
                    "8095554444",
                Role =
                    AppUserRole.Collector
            };

        var response =
            await context.Client.PutAsJsonAsync(
                $"/api/app-users/{created.Id}",
                request);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task Reactivate_InactiveUser_ReturnsActiveUser()
    {
        var context =
            await CreateContextAsync();

        var created =
            await CreateAppUserAndGetAsync(
                context.Client,
                "Usuario Reactivar",
                Unique("reactivate"),
                AppUserRole.Collector);

        var deleteResponse =
            await context.Client.DeleteAsync(
                $"/api/app-users/{created.Id}");

        Assert.Equal(
            HttpStatusCode.NoContent,
            deleteResponse.StatusCode);

        var response =
            await context.Client.PatchAsync(
                $"/api/app-users/{created.Id}/reactivate",
                null);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var user =
            await response.Content
                .ReadFromJsonAsync<AppUserResponse>();

        Assert.NotNull(user);
        Assert.True(user.IsActive);
    }

    // ============================================================
    // LAST ACTIVE ADMINISTRATOR
    // ============================================================

    [Fact]
    public async Task Delete_LastActiveAdministrator_ReturnsBadRequest()
    {
        var context =
            await CreateContextAsync();

        var response =
            await context.Client.DeleteAsync(
                $"/api/app-users/" +
                $"{context.AdministratorId}");

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task Update_LastActiveAdministrator_ToCollector_ReturnsBadRequest()
    {
        var context =
            await CreateContextAsync();

        var request =
            new UpdateAppUserRequest
            {
                Name =
                    "Administrador Test",
                Username =
                    context.Username,
                Email =
                    "admin@test.com",
                Phone =
                    "8095555678",
                Role =
                    AppUserRole.Collector
            };

        var response =
            await context.Client.PutAsJsonAsync(
                $"/api/app-users/" +
                $"{context.AdministratorId}",
                request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task Delete_Administrator_WhenAnotherActiveAdministratorExists_ReturnsNoContent()
    {
        var context =
            await CreateContextAsync();

        var secondAdmin =
            await CreateAppUserAndGetAsync(
                context.Client,
                "Segundo Administrador",
                Unique("admin2"),
                AppUserRole.Administrator);

        Assert.NotEqual(
            context.AdministratorId,
            secondAdmin.Id);

        var response =
            await context.Client.DeleteAsync(
                $"/api/app-users/" +
                $"{context.AdministratorId}");

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);
    }

    [Fact]
    public async Task
        Update_AdministratorToCollector_WhenAnotherActiveAdministratorExists_ReturnsOk()
    {
        var context =
            await CreateContextAsync();

        var secondAdmin =
            await CreateAppUserAndGetAsync(
                context.Client,
                "Segundo Administrador Cambio Rol",
                Unique("admin2role"),
                AppUserRole.Administrator);

        Assert.NotEqual(
            context.AdministratorId,
            secondAdmin.Id);

        var request =
            new UpdateAppUserRequest
            {
                Name =
                    secondAdmin.Name,

                Username =
                    secondAdmin.Username,

                Email =
                    secondAdmin.Email,

                Phone =
                    secondAdmin.Phone,

                Role =
                    AppUserRole.Collector
            };

        var response =
            await context.Client.PutAsJsonAsync(
                $"/api/app-users/{secondAdmin.Id}",
                request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var updated =
            await response.Content
                .ReadFromJsonAsync<AppUserResponse>();

        Assert.NotNull(updated);

        Assert.Equal(
            AppUserRole.Collector,
            updated.Role);

        Assert.True(
            updated.IsActive);
    }

    // ============================================================
    // COLLECTION ROUTE ASSIGNMENT
    // ============================================================

    [Fact]
    public async Task AssignCollectionRoute_ToCollector_ReturnsNoContent()
    {
        var context =
            await CreateContextAsync();

        var collector =
            await CreateAppUserAndGetAsync(
                context.Client,
                "Cobrador Ruta",
                Unique("routecollector"),
                AppUserRole.Collector);

        var route =
            await CreateCollectionRouteAsync(
                context.Client);

        var response =
            await context.Client.PostAsync(
                $"/api/app-users/{collector.Id}" +
                $"/collection-routes/{route.Id}",
                null);

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);
    }

    [Fact]
    public async Task GetCollectionRoutes_ReturnsAssignedRoute()
    {
        var context =
            await CreateContextAsync();

        var collector =
            await CreateAppUserAndGetAsync(
                context.Client,
                "Cobrador Consulta Ruta",
                Unique("getroute"),
                AppUserRole.Collector);

        var route =
            await CreateCollectionRouteAsync(
                context.Client);

        var assignResponse =
            await context.Client.PostAsync(
                $"/api/app-users/{collector.Id}" +
                $"/collection-routes/{route.Id}",
                null);

        Assert.Equal(
            HttpStatusCode.NoContent,
            assignResponse.StatusCode);

        var response =
            await context.Client.GetAsync(
                $"/api/app-users/{collector.Id}" +
                "/collection-routes");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var routes =
            await response.Content
                .ReadFromJsonAsync<
                    List<AppUserCollectionRouteResponse>>();

        Assert.NotNull(routes);

        var assignment =
            Assert.Single(
                routes,
                x =>
                    x.CollectionRouteId ==
                    route.Id);

        Assert.Equal(
            route.Name,
            assignment.CollectionRouteName);

        Assert.NotEqual(
            default,
            assignment.AssignedAt);
    }

    [Fact]
    public async Task AssignCollectionRoute_Duplicate_ReturnsBadRequest()
    {
        var context =
            await CreateContextAsync();

        var collector =
            await CreateAppUserAndGetAsync(
                context.Client,
                "Cobrador Duplicado",
                Unique("duplicateroute"),
                AppUserRole.Collector);

        var route =
            await CreateCollectionRouteAsync(
                context.Client);

        var firstResponse =
            await context.Client.PostAsync(
                $"/api/app-users/{collector.Id}" +
                $"/collection-routes/{route.Id}",
                null);

        Assert.Equal(
            HttpStatusCode.NoContent,
            firstResponse.StatusCode);

        var secondResponse =
            await context.Client.PostAsync(
                $"/api/app-users/{collector.Id}" +
                $"/collection-routes/{route.Id}",
                null);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            secondResponse.StatusCode);
    }

    [Fact]
    public async Task AssignCollectionRoute_ToAdministrator_ReturnsBadRequest()
    {
        var context =
            await CreateContextAsync();

        var administrator =
            await CreateAppUserAndGetAsync(
                context.Client,
                "Administrador Sin Ruta",
                Unique("adminroute"),
                AppUserRole.Administrator);

        var route =
            await CreateCollectionRouteAsync(
                context.Client);

        var response =
            await context.Client.PostAsync(
                $"/api/app-users/{administrator.Id}" +
                $"/collection-routes/{route.Id}",
                null);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task AssignCollectionRoute_FromDifferentTenant_ReturnsBadRequest()
    {
        var tenant1 =
            await CreateContextAsync();

        var tenant2 =
            await CreateContextAsync();

        var collector =
            await CreateAppUserAndGetAsync(
                tenant1.Client,
                "Cobrador Tenant Uno",
                Unique("crossroute"),
                AppUserRole.Collector);

        var route =
            await CreateCollectionRouteAsync(
                tenant2.Client);

        var response =
            await tenant1.Client.PostAsync(
                $"/api/app-users/{collector.Id}" +
                $"/collection-routes/{route.Id}",
                null);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task AssignCollectionRoute_InactiveRoute_ReturnsBadRequest()
    {
        var context =
            await CreateContextAsync();

        var collector =
            await CreateAppUserAndGetAsync(
                context.Client,
                "Cobrador Ruta Inactiva",
                Unique("inactiveroute"),
                AppUserRole.Collector);

        var route =
            await CreateCollectionRouteAsync(
                context.Client);

        var deleteRouteResponse =
            await context.Client.DeleteAsync(
                $"/api/collection-routes/{route.Id}");

        Assert.Equal(
            HttpStatusCode.NoContent,
            deleteRouteResponse.StatusCode);

        var response =
            await context.Client.PostAsync(
                $"/api/app-users/{collector.Id}" +
                $"/collection-routes/{route.Id}",
                null);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task UnassignCollectionRoute_RemovesAssignment()
    {
        var context =
            await CreateContextAsync();

        var collector =
            await CreateAppUserAndGetAsync(
                context.Client,
                "Cobrador Quitar Ruta",
                Unique("unassign"),
                AppUserRole.Collector);

        var route =
            await CreateCollectionRouteAsync(
                context.Client);

        var assignResponse =
            await context.Client.PostAsync(
                $"/api/app-users/{collector.Id}" +
                $"/collection-routes/{route.Id}",
                null);

        Assert.Equal(
            HttpStatusCode.NoContent,
            assignResponse.StatusCode);

        var deleteResponse =
            await context.Client.DeleteAsync(
                $"/api/app-users/{collector.Id}" +
                $"/collection-routes/{route.Id}");

        Assert.Equal(
            HttpStatusCode.NoContent,
            deleteResponse.StatusCode);

        var getResponse =
            await context.Client.GetAsync(
                $"/api/app-users/{collector.Id}" +
                "/collection-routes");

        var routes =
            await getResponse.Content
                .ReadFromJsonAsync<
                    List<AppUserCollectionRouteResponse>>();

        Assert.NotNull(routes);

        Assert.DoesNotContain(
            routes,
            x =>
                x.CollectionRouteId ==
                route.Id);
    }

    [Fact]
    public async Task GetCollectionRoutes_UserFromDifferentTenant_ReturnsNotFound()
    {
        var tenant1 =
            await CreateContextAsync();

        var tenant2 =
            await CreateContextAsync();

        var collector =
            await CreateAppUserAndGetAsync(
                tenant1.Client,
                "Cobrador Aislamiento",
                Unique("routeisolation"),
                AppUserRole.Collector);

        var route =
            await CreateCollectionRouteAsync(
                tenant1.Client);

        await tenant1.Client.PostAsync(
            $"/api/app-users/{collector.Id}" +
            $"/collection-routes/{route.Id}",
            null);

        var response =
            await tenant2.Client.GetAsync(
                $"/api/app-users/{collector.Id}" +
                "/collection-routes");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    // ============================================================
    // ROLE CHANGE WITH ASSIGNED ROUTES
    // ============================================================

    [Fact]
    public async Task Update_CollectorWithAssignedRoute_ToAdministrator_ReturnsBadRequest()
    {
        var context =
            await CreateContextAsync();

        var collector =
            await CreateAppUserAndGetAsync(
                context.Client,
                "Cobrador Cambio Role",
                Unique("rolechange"),
                AppUserRole.Collector);

        var route =
            await CreateCollectionRouteAsync(
                context.Client);

        var assignResponse =
            await context.Client.PostAsync(
                $"/api/app-users/{collector.Id}" +
                $"/collection-routes/{route.Id}",
                null);

        Assert.Equal(
            HttpStatusCode.NoContent,
            assignResponse.StatusCode);

        var request =
            new UpdateAppUserRequest
            {
                Name =
                    collector.Name,
                Username =
                    collector.Username,
                Email =
                    collector.Email,
                Phone =
                    collector.Phone,
                Role =
                    AppUserRole.Administrator
            };

        var response =
            await context.Client.PutAsJsonAsync(
                $"/api/app-users/{collector.Id}",
                request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task Update_CollectorAfterUnassigningRoutes_ToAdministrator_ReturnsOk()
    {
        var context =
            await CreateContextAsync();

        var collector =
            await CreateAppUserAndGetAsync(
                context.Client,
                "Cobrador Cambiable",
                Unique("roleallowed"),
                AppUserRole.Collector);

        var route =
            await CreateCollectionRouteAsync(
                context.Client);

        await context.Client.PostAsync(
            $"/api/app-users/{collector.Id}" +
            $"/collection-routes/{route.Id}",
            null);

        var removeResponse =
            await context.Client.DeleteAsync(
                $"/api/app-users/{collector.Id}" +
                $"/collection-routes/{route.Id}");

        Assert.Equal(
            HttpStatusCode.NoContent,
            removeResponse.StatusCode);

        var request =
            new UpdateAppUserRequest
            {
                Name =
                    collector.Name,
                Username =
                    collector.Username,
                Email =
                    collector.Email,
                Phone =
                    collector.Phone,
                Role =
                    AppUserRole.Administrator
            };

        var response =
            await context.Client.PutAsJsonAsync(
                $"/api/app-users/{collector.Id}",
                request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var updated =
            await response.Content
                .ReadFromJsonAsync<AppUserResponse>();

        Assert.NotNull(updated);

        Assert.Equal(
            AppUserRole.Administrator,
            updated.Role);
    }

    // ============================================================
    // ASSIGNMENTS + SOFT DELETE
    // ============================================================

    [Fact]
    public async Task SoftDeleteAndReactivate_PreservesRouteAssignment()
    {
        var context =
            await CreateContextAsync();

        var collector =
            await CreateAppUserAndGetAsync(
                context.Client,
                "Cobrador Persistencia",
                Unique("persist"),
                AppUserRole.Collector);

        var route =
            await CreateCollectionRouteAsync(
                context.Client);

        var assignResponse =
            await context.Client.PostAsync(
                $"/api/app-users/{collector.Id}" +
                $"/collection-routes/{route.Id}",
                null);

        Assert.Equal(
            HttpStatusCode.NoContent,
            assignResponse.StatusCode);

        var deleteResponse =
            await context.Client.DeleteAsync(
                $"/api/app-users/{collector.Id}");

        Assert.Equal(
            HttpStatusCode.NoContent,
            deleteResponse.StatusCode);

        var inactiveRoutesResponse =
            await context.Client.GetAsync(
                $"/api/app-users/{collector.Id}" +
                "/collection-routes");

        Assert.Equal(
            HttpStatusCode.NotFound,
            inactiveRoutesResponse.StatusCode);

        var reactivateResponse =
            await context.Client.PatchAsync(
                $"/api/app-users/{collector.Id}/reactivate",
                null);

        Assert.Equal(
            HttpStatusCode.OK,
            reactivateResponse.StatusCode);

        var routesResponse =
            await context.Client.GetAsync(
                $"/api/app-users/{collector.Id}" +
                "/collection-routes");

        Assert.Equal(
            HttpStatusCode.OK,
            routesResponse.StatusCode);

        var routes =
            await routesResponse.Content
                .ReadFromJsonAsync<
                    List<AppUserCollectionRouteResponse>>();

        Assert.NotNull(routes);

        Assert.Contains(
            routes,
            x =>
                x.CollectionRouteId ==
                route.Id);
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
        CreateAppUserAsync(
            HttpClient client,
            string name,
            string username,
            AppUserRole role,
            string? email = null,
            string? phone = null,
            string password =
                TestAuthenticationHelper
                    .DefaultPassword)
    {
        var request =
            new CreateAppUserRequest
            {
                Name = name,
                Username = username,
                Password = password,
                Email =
                    email ??
                    $"{Guid.NewGuid():N}@test.com",
                Phone =
                    phone ??
                    "8095551234",
                Role = role
            };

        return await client.PostAsJsonAsync(
            "/api/app-users",
            request);
    }

    private static async Task<AppUserResponse>
        CreateAppUserAndGetAsync(
            HttpClient client,
            string name,
            string username,
            AppUserRole role)
    {
        var response =
            await CreateAppUserAsync(
                client,
                name,
                username,
                role);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var appUser =
            await response.Content
                .ReadFromJsonAsync<AppUserResponse>();

        Assert.NotNull(appUser);

        return appUser;
    }

    private static async Task<CollectionRouteResponse>
        CreateCollectionRouteAsync(
            HttpClient client)
    {
        var suffix =
            Guid.NewGuid().ToString("N");

        var request =
            new CreateCollectionRouteRequest
            {
                Name =
                    $"Ruta Test {suffix}",
                Description =
                    "Ruta creada por AppUsersTests",
                OrderMode =
                    CollectionRouteOrderMode.Manual
            };

        var response =
            await client.PostAsJsonAsync(
                "/api/collection-routes",
                request);

        response.EnsureSuccessStatusCode();

        var route =
            await response.Content
                .ReadFromJsonAsync<
                    CollectionRouteResponse>();

        Assert.NotNull(route);

        return route;
    }

    private static string Unique(
        string prefix)
    {
        return
            $"{prefix}_{Guid.NewGuid():N}";
    }
}