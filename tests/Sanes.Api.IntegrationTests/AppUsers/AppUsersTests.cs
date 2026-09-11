using System.Net;
using System.Net.Http.Json;
using Sanes.Application.AppUsers.DTOs;
using Sanes.Application.CollectionRoutes.DTOs;
using Sanes.Application.Tenants.DTOs;
using Sanes.Domain.Enums;
using System.Text.Json;

namespace Sanes.Api.IntegrationTests.AppUsers;

public class AppUsersTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AppUsersTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ============================================================
    // CREATE
    // ============================================================

    [Fact]
    public async Task Create_Administrator_ReturnsCreated()
    {
        var tenantId = await CreateTenantAsync();

        var username = Unique("admin");

        var response = await CreateAppUserAsync(
            tenantId,
            name: "Administrador Test",
            username: username,
            role: AppUserRole.Administrator);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var appUser =
            await response.Content
                .ReadFromJsonAsync<AppUserResponse>();

        Assert.NotNull(appUser);
        Assert.Equal(tenantId, appUser.TenantId);
        Assert.Equal("Administrador Test", appUser.Name);
        Assert.Equal(username.ToLowerInvariant(), appUser.Username);
        Assert.Equal(AppUserRole.Administrator, appUser.Role);
        Assert.True(appUser.IsActive);
    }

    [Fact]
    public async Task Create_Collector_ReturnsCreated()
    {
        var tenantId = await CreateTenantAsync();

        var response = await CreateAppUserAsync(
            tenantId,
            name: "Cobrador Test",
            username: Unique("collector"),
            role: AppUserRole.Collector);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var appUser =
            await response.Content
                .ReadFromJsonAsync<AppUserResponse>();

        Assert.NotNull(appUser);
        Assert.Equal(AppUserRole.Collector, appUser.Role);
        Assert.True(appUser.IsActive);
    }

    [Fact]
    public async Task Create_NormalizesUsername()
    {
        var tenantId = await CreateTenantAsync();

        var rawUsername =
            $"  USER_{Guid.NewGuid():N}  ";

        var response = await CreateAppUserAsync(
            tenantId,
            name: "Usuario Normalizado",
            username: rawUsername,
            role: AppUserRole.Collector);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var appUser =
            await response.Content
                .ReadFromJsonAsync<AppUserResponse>();

        Assert.NotNull(appUser);

        Assert.Equal(
            rawUsername.Trim().ToLowerInvariant(),
            appUser.Username);
    }

    [Fact]
    public async Task Create_DuplicateUsernameSameTenant_ReturnsBadRequest()
    {
        var tenantId = await CreateTenantAsync();

        var username = Unique("duplicate");

        var firstResponse = await CreateAppUserAsync(
            tenantId,
            name: "Usuario Uno",
            username: username,
            role: AppUserRole.Collector);

        Assert.Equal(
            HttpStatusCode.Created,
            firstResponse.StatusCode);

        var secondResponse = await CreateAppUserAsync(
            tenantId,
            name: "Usuario Dos",
            username: username.ToUpperInvariant(),
            role: AppUserRole.Collector);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            secondResponse.StatusCode);
    }

    [Fact]
    public async Task Create_SameUsernameDifferentTenant_ReturnsCreated()
    {
        var tenant1 = await CreateTenantAsync();
        var tenant2 = await CreateTenantAsync();

        var username = Unique("shared");

        var firstResponse = await CreateAppUserAsync(
            tenant1,
            "Usuario Tenant 1",
            username,
            AppUserRole.Collector);

        var secondResponse = await CreateAppUserAsync(
            tenant2,
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
        var tenantId = await CreateTenantAsync();

        var request = new
        {
            tenantId,
            name = "Usuario Email",
            username = Unique("email"),
            email = "correo-invalido",
            phone = "8095551234",
            role = (int)AppUserRole.Collector
        };

        var response =
            await _client.PostAsJsonAsync(
                "/api/app-users",
                request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task Create_InvalidRole_ReturnsBadRequest()
    {
        var tenantId = await CreateTenantAsync();

        var request = new
        {
            tenantId,
            name = "Usuario Role",
            username = Unique("role"),
            email = "role@test.com",
            phone = "8095551234",
            role = 99
        };

        var response =
            await _client.PostAsJsonAsync(
                "/api/app-users",
                request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task Create_NonExistingTenant_ReturnsBadRequest()
    {
        var request = new
        {
            tenantId = Guid.NewGuid(),
            name = "Usuario Sin Tenant",
            username = Unique("notenant"),
            email = "notenant@test.com",
            phone = "8095551234",
            role = (int)AppUserRole.Collector
        };

        var response =
            await _client.PostAsJsonAsync(
                "/api/app-users",
                request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task Create_InactiveTenant_ReturnsBadRequest()
    {
        var tenantId = await CreateTenantAsync();

        var deleteResponse =
            await _client.DeleteAsync(
                $"/api/tenants/{tenantId}");

        Assert.Equal(
            HttpStatusCode.NoContent,
            deleteResponse.StatusCode);

        var response = await CreateAppUserAsync(
            tenantId,
            "Usuario Tenant Inactivo",
            Unique("inactive"),
            AppUserRole.Collector);

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
        var tenantId = await CreateTenantAsync();

        var username1 = Unique("getall1");
        var username2 = Unique("getall2");

        await CreateAppUserAndGetAsync(
            tenantId,
            "Usuario Uno",
            username1,
            AppUserRole.Administrator);

        await CreateAppUserAndGetAsync(
            tenantId,
            "Usuario Dos",
            username2,
            AppUserRole.Collector);

        var response =
            await _client.GetAsync(
                $"/api/app-users?tenantId={tenantId}");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var users =
            await response.Content
                .ReadFromJsonAsync<List<AppUserResponse>>();

        Assert.NotNull(users);

        Assert.Contains(
            users,
            x => x.Username == username1.ToLowerInvariant());

        Assert.Contains(
            users,
            x => x.Username == username2.ToLowerInvariant());
    }

    [Fact]
    public async Task GetAll_WithCollectorRole_ReturnsOnlyCollectors()
    {
        var tenantId = await CreateTenantAsync();

        var collector =
            await CreateAppUserAndGetAsync(
                tenantId,
                "Cobrador Filtro",
                Unique("collectorfilter"),
                AppUserRole.Collector);

        await CreateAppUserAndGetAsync(
            tenantId,
            "Administrador Filtro",
            Unique("adminfilter"),
            AppUserRole.Administrator);

        var response =
            await _client.GetAsync(
                $"/api/app-users?tenantId={tenantId}&role={(int)AppUserRole.Collector}");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var users =
            await response.Content
                .ReadFromJsonAsync<List<AppUserResponse>>();

        Assert.NotNull(users);
        Assert.Contains(users, x => x.Id == collector.Id);
        Assert.All(
            users,
            x => Assert.Equal(
                AppUserRole.Collector,
                x.Role));
    }

    [Fact]
    public async Task GetAll_WithInvalidRole_ReturnsBadRequest()
    {
        var tenantId = await CreateTenantAsync();

        var response =
            await _client.GetAsync(
                $"/api/app-users?tenantId={tenantId}&role=99");

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task GetById_ExistingUser_ReturnsUser()
    {
        var tenantId = await CreateTenantAsync();

        var created =
            await CreateAppUserAndGetAsync(
                tenantId,
                "Usuario Detalle",
                Unique("detail"),
                AppUserRole.Collector);

        var response =
            await _client.GetAsync(
                $"/api/app-users/{created.Id}?tenantId={tenantId}");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var user =
            await response.Content
                .ReadFromJsonAsync<AppUserResponse>();

        Assert.NotNull(user);
        Assert.Equal(created.Id, user.Id);
        Assert.Equal(tenantId, user.TenantId);
    }

    [Fact]
    public async Task GetById_WithDifferentTenant_ReturnsNotFound()
    {
        var tenant1 = await CreateTenantAsync();
        var tenant2 = await CreateTenantAsync();

        var created =
            await CreateAppUserAndGetAsync(
                tenant1,
                "Usuario Tenant Isolation",
                Unique("isolation"),
                AppUserRole.Collector);

        var response =
            await _client.GetAsync(
                $"/api/app-users/{created.Id}?tenantId={tenant2}");

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
        var tenantId = await CreateTenantAsync();

        var created =
            await CreateAppUserAndGetAsync(
                tenantId,
                "Usuario Original",
                Unique("update"),
                AppUserRole.Collector);

        var newUsername = Unique("updated");

        var request = new
        {
            name = "Usuario Actualizado",
            username = newUsername,
            email = "actualizado@test.com",
            phone = "8095559999",
            role = (int)AppUserRole.Collector
        };

        var response =
            await _client.PutAsJsonAsync(
                $"/api/app-users/{created.Id}?tenantId={tenantId}",
                request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var updated =
            await response.Content
                .ReadFromJsonAsync<AppUserResponse>();

        Assert.NotNull(updated);
        Assert.Equal("Usuario Actualizado", updated.Name);
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
        var tenantId = await CreateTenantAsync();

        var created =
            await CreateAppUserAndGetAsync(
                tenantId,
                "Usuario Mismo Username",
                Unique("same"),
                AppUserRole.Collector);

        var request = new
        {
            name = "Nombre Actualizado",
            username = created.Username.ToUpperInvariant(),
            email = "same@test.com",
            phone = "8095551111",
            role = (int)AppUserRole.Collector
        };

        var response =
            await _client.PutAsJsonAsync(
                $"/api/app-users/{created.Id}?tenantId={tenantId}",
                request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);
    }

    [Fact]
    public async Task Update_WithDuplicateUsername_ReturnsBadRequest()
    {
        var tenantId = await CreateTenantAsync();

        var user1 =
            await CreateAppUserAndGetAsync(
                tenantId,
                "Usuario Uno",
                Unique("dup1"),
                AppUserRole.Collector);

        var user2 =
            await CreateAppUserAndGetAsync(
                tenantId,
                "Usuario Dos",
                Unique("dup2"),
                AppUserRole.Collector);

        var request = new
        {
            name = user2.Name,
            username = user1.Username,
            email = "duplicate@test.com",
            phone = "8095552222",
            role = (int)AppUserRole.Collector
        };

        var response =
            await _client.PutAsJsonAsync(
                $"/api/app-users/{user2.Id}?tenantId={tenantId}",
                request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task Update_WithDifferentTenant_ReturnsNotFound()
    {
        var tenant1 = await CreateTenantAsync();
        var tenant2 = await CreateTenantAsync();

        var created =
            await CreateAppUserAndGetAsync(
                tenant1,
                "Usuario Update Tenant",
                Unique("updatetenant"),
                AppUserRole.Collector);

        var request = new
        {
            name = "Intento Otro Tenant",
            username = created.Username,
            email = "tenant@test.com",
            phone = "8095553333",
            role = (int)AppUserRole.Collector
        };

        var response =
            await _client.PutAsJsonAsync(
                $"/api/app-users/{created.Id}?tenantId={tenant2}",
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
        var tenantId = await CreateTenantAsync();

        var created =
            await CreateAppUserAndGetAsync(
                tenantId,
                "Usuario Delete",
                Unique("delete"),
                AppUserRole.Collector);

        var response =
            await _client.DeleteAsync(
                $"/api/app-users/{created.Id}?tenantId={tenantId}");

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        var getResponse =
            await _client.GetAsync(
                $"/api/app-users/{created.Id}?tenantId={tenantId}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_UserDisappearsFromActiveList()
    {
        var tenantId = await CreateTenantAsync();

        var created =
            await CreateAppUserAndGetAsync(
                tenantId,
                "Usuario Lista Delete",
                Unique("listdelete"),
                AppUserRole.Collector);

        var deleteResponse =
            await _client.DeleteAsync(
                $"/api/app-users/{created.Id}?tenantId={tenantId}");

        Assert.Equal(
            HttpStatusCode.NoContent,
            deleteResponse.StatusCode);

        var listResponse =
            await _client.GetAsync(
                $"/api/app-users?tenantId={tenantId}");

        var users =
            await listResponse.Content
                .ReadFromJsonAsync<List<AppUserResponse>>();

        Assert.NotNull(users);
        Assert.DoesNotContain(
            users,
            x => x.Id == created.Id);
    }

    [Fact]
    public async Task Update_InactiveUser_ReturnsNotFound()
    {
        var tenantId = await CreateTenantAsync();

        var created =
            await CreateAppUserAndGetAsync(
                tenantId,
                "Usuario Inactivo",
                Unique("inactiveupdate"),
                AppUserRole.Collector);

        await _client.DeleteAsync(
            $"/api/app-users/{created.Id}?tenantId={tenantId}");

        var request = new
        {
            name = "No Debe Actualizar",
            username = created.Username,
            email = "inactive@test.com",
            phone = "8095554444",
            role = (int)AppUserRole.Collector
        };

        var response =
            await _client.PutAsJsonAsync(
                $"/api/app-users/{created.Id}?tenantId={tenantId}",
                request);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task Reactivate_InactiveUser_ReturnsActiveUser()
    {
        var tenantId = await CreateTenantAsync();

        var created =
            await CreateAppUserAndGetAsync(
                tenantId,
                "Usuario Reactivar",
                Unique("reactivate"),
                AppUserRole.Collector);

        var deleteResponse =
            await _client.DeleteAsync(
                $"/api/app-users/{created.Id}?tenantId={tenantId}");

        Assert.Equal(
            HttpStatusCode.NoContent,
            deleteResponse.StatusCode);

        var response =
            await _client.PatchAsync(
                $"/api/app-users/{created.Id}/reactivate?tenantId={tenantId}",
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
    // COLLECTION ROUTE ASSIGNMENT
    // ============================================================

    [Fact]
    public async Task AssignCollectionRoute_ToCollector_ReturnsNoContent()
    {
        var tenantId = await CreateTenantAsync();

        var collector =
            await CreateAppUserAndGetAsync(
                tenantId,
                "Cobrador Ruta",
                Unique("routecollector"),
                AppUserRole.Collector);

        var route =
            await CreateCollectionRouteAsync(
                tenantId);

        var response =
            await _client.PostAsync(
                $"/api/app-users/{collector.Id}/collection-routes/{route.Id}?tenantId={tenantId}",
                null);

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);
    }

    [Fact]
    public async Task GetCollectionRoutes_ReturnsAssignedRoute()
    {
        var tenantId = await CreateTenantAsync();

        var collector =
            await CreateAppUserAndGetAsync(
                tenantId,
                "Cobrador Consulta Ruta",
                Unique("getroute"),
                AppUserRole.Collector);

        var route =
            await CreateCollectionRouteAsync(
                tenantId);

        var assignResponse =
            await _client.PostAsync(
                $"/api/app-users/{collector.Id}/collection-routes/{route.Id}?tenantId={tenantId}",
                null);

        Assert.Equal(
            HttpStatusCode.NoContent,
            assignResponse.StatusCode);

        var response =
            await _client.GetAsync(
                $"/api/app-users/{collector.Id}/collection-routes?tenantId={tenantId}");

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
                x => x.CollectionRouteId == route.Id);

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
        var tenantId = await CreateTenantAsync();

        var collector =
            await CreateAppUserAndGetAsync(
                tenantId,
                "Cobrador Duplicado",
                Unique("duplicateroute"),
                AppUserRole.Collector);

        var route =
            await CreateCollectionRouteAsync(
                tenantId);

        var firstResponse =
            await _client.PostAsync(
                $"/api/app-users/{collector.Id}/collection-routes/{route.Id}?tenantId={tenantId}",
                null);

        Assert.Equal(
            HttpStatusCode.NoContent,
            firstResponse.StatusCode);

        var secondResponse =
            await _client.PostAsync(
                $"/api/app-users/{collector.Id}/collection-routes/{route.Id}?tenantId={tenantId}",
                null);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            secondResponse.StatusCode);
    }

    [Fact]
    public async Task AssignCollectionRoute_ToAdministrator_ReturnsBadRequest()
    {
        var tenantId = await CreateTenantAsync();

        var administrator =
            await CreateAppUserAndGetAsync(
                tenantId,
                "Administrador Sin Ruta",
                Unique("adminroute"),
                AppUserRole.Administrator);

        var route =
            await CreateCollectionRouteAsync(
                tenantId);

        var response =
            await _client.PostAsync(
                $"/api/app-users/{administrator.Id}/collection-routes/{route.Id}?tenantId={tenantId}",
                null);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task AssignCollectionRoute_FromDifferentTenant_ReturnsBadRequest()
    {
        var tenant1 = await CreateTenantAsync();
        var tenant2 = await CreateTenantAsync();

        var collector =
            await CreateAppUserAndGetAsync(
                tenant1,
                "Cobrador Tenant Uno",
                Unique("crossroute"),
                AppUserRole.Collector);

        var route =
            await CreateCollectionRouteAsync(
                tenant2);

        var response =
            await _client.PostAsync(
                $"/api/app-users/{collector.Id}/collection-routes/{route.Id}?tenantId={tenant1}",
                null);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task AssignCollectionRoute_InactiveRoute_ReturnsBadRequest()
    {
        var tenantId = await CreateTenantAsync();

        var collector =
            await CreateAppUserAndGetAsync(
                tenantId,
                "Cobrador Ruta Inactiva",
                Unique("inactiveroute"),
                AppUserRole.Collector);

        var route =
            await CreateCollectionRouteAsync(
                tenantId);

        var deleteRouteResponse =
            await _client.DeleteAsync(
                $"/api/collection-routes/{route.Id}?tenantId={tenantId}");

        Assert.Equal(
            HttpStatusCode.NoContent,
            deleteRouteResponse.StatusCode);

        var response =
            await _client.PostAsync(
                $"/api/app-users/{collector.Id}/collection-routes/{route.Id}?tenantId={tenantId}",
                null);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task UnassignCollectionRoute_RemovesAssignment()
    {
        var tenantId = await CreateTenantAsync();

        var collector =
            await CreateAppUserAndGetAsync(
                tenantId,
                "Cobrador Quitar Ruta",
                Unique("unassign"),
                AppUserRole.Collector);

        var route =
            await CreateCollectionRouteAsync(
                tenantId);

        var assignResponse =
            await _client.PostAsync(
                $"/api/app-users/{collector.Id}/collection-routes/{route.Id}?tenantId={tenantId}",
                null);

        Assert.Equal(
            HttpStatusCode.NoContent,
            assignResponse.StatusCode);

        var deleteResponse =
            await _client.DeleteAsync(
                $"/api/app-users/{collector.Id}/collection-routes/{route.Id}?tenantId={tenantId}");

        Assert.Equal(
            HttpStatusCode.NoContent,
            deleteResponse.StatusCode);

        var getResponse =
            await _client.GetAsync(
                $"/api/app-users/{collector.Id}/collection-routes?tenantId={tenantId}");

        var routes =
            await getResponse.Content
                .ReadFromJsonAsync<
                    List<AppUserCollectionRouteResponse>>();

        Assert.NotNull(routes);

        Assert.DoesNotContain(
            routes,
            x => x.CollectionRouteId == route.Id);
    }

    [Fact]
    public async Task GetCollectionRoutes_WithDifferentTenant_ReturnsNotFound()
    {
        var tenant1 = await CreateTenantAsync();
        var tenant2 = await CreateTenantAsync();

        var collector =
            await CreateAppUserAndGetAsync(
                tenant1,
                "Cobrador Aislamiento",
                Unique("routeisolation"),
                AppUserRole.Collector);

        var route =
            await CreateCollectionRouteAsync(
                tenant1);

        await _client.PostAsync(
            $"/api/app-users/{collector.Id}/collection-routes/{route.Id}?tenantId={tenant1}",
            null);

        var response =
            await _client.GetAsync(
                $"/api/app-users/{collector.Id}/collection-routes?tenantId={tenant2}");

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
        var tenantId = await CreateTenantAsync();

        var collector =
            await CreateAppUserAndGetAsync(
                tenantId,
                "Cobrador Cambio Role",
                Unique("rolechange"),
                AppUserRole.Collector);

        var route =
            await CreateCollectionRouteAsync(
                tenantId);

        var assignResponse =
            await _client.PostAsync(
                $"/api/app-users/{collector.Id}/collection-routes/{route.Id}?tenantId={tenantId}",
                null);

        Assert.Equal(
            HttpStatusCode.NoContent,
            assignResponse.StatusCode);

        var request = new
        {
            name = collector.Name,
            username = collector.Username,
            email = collector.Email,
            phone = collector.Phone,
            role = (int)AppUserRole.Administrator
        };

        var response =
            await _client.PutAsJsonAsync(
                $"/api/app-users/{collector.Id}?tenantId={tenantId}",
                request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task Update_CollectorAfterUnassigningRoutes_ToAdministrator_ReturnsOk()
    {
        var tenantId = await CreateTenantAsync();

        var collector =
            await CreateAppUserAndGetAsync(
                tenantId,
                "Cobrador Cambiable",
                Unique("roleallowed"),
                AppUserRole.Collector);

        var route =
            await CreateCollectionRouteAsync(
                tenantId);

        await _client.PostAsync(
            $"/api/app-users/{collector.Id}/collection-routes/{route.Id}?tenantId={tenantId}",
            null);

        var removeResponse =
            await _client.DeleteAsync(
                $"/api/app-users/{collector.Id}/collection-routes/{route.Id}?tenantId={tenantId}");

        Assert.Equal(
            HttpStatusCode.NoContent,
            removeResponse.StatusCode);

        var request = new
        {
            name = collector.Name,
            username = collector.Username,
            email = collector.Email,
            phone = collector.Phone,
            role = (int)AppUserRole.Administrator
        };

        var response =
            await _client.PutAsJsonAsync(
                $"/api/app-users/{collector.Id}?tenantId={tenantId}",
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
        var tenantId = await CreateTenantAsync();

        var collector =
            await CreateAppUserAndGetAsync(
                tenantId,
                "Cobrador Persistencia",
                Unique("persist"),
                AppUserRole.Collector);

        var route =
            await CreateCollectionRouteAsync(
                tenantId);

        var assignResponse =
            await _client.PostAsync(
                $"/api/app-users/{collector.Id}/collection-routes/{route.Id}?tenantId={tenantId}",
                null);

        Assert.Equal(
            HttpStatusCode.NoContent,
            assignResponse.StatusCode);

        var deleteResponse =
            await _client.DeleteAsync(
                $"/api/app-users/{collector.Id}?tenantId={tenantId}");

        Assert.Equal(
            HttpStatusCode.NoContent,
            deleteResponse.StatusCode);

        var inactiveRoutesResponse =
            await _client.GetAsync(
                $"/api/app-users/{collector.Id}/collection-routes?tenantId={tenantId}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            inactiveRoutesResponse.StatusCode);

        var reactivateResponse =
            await _client.PatchAsync(
                $"/api/app-users/{collector.Id}/reactivate?tenantId={tenantId}",
                null);

        Assert.Equal(
            HttpStatusCode.OK,
            reactivateResponse.StatusCode);

        var routesResponse =
            await _client.GetAsync(
                $"/api/app-users/{collector.Id}/collection-routes?tenantId={tenantId}");

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
            x => x.CollectionRouteId == route.Id);
    }

    // ============================================================
    // HELPERS
    // ============================================================

    private async Task<Guid> CreateTenantAsync()
    {
        var suffix = Guid.NewGuid().ToString("N");

        var request = new
        {
            name = $"Tenant AppUser {suffix}",
            legalName = $"Tenant AppUser SRL {suffix}",
            phone = "8095551234",
            email = $"tenant-{suffix}@test.com",
            currencyCode = "DOP",
            currencySymbol = "RD$"
        };

        var response =
            await _client.PostAsJsonAsync(
                "/api/tenants",
                request);

        response.EnsureSuccessStatusCode();

        var json =
            await response.Content
                .ReadFromJsonAsync<JsonElement>();

        var tenantId =
            json.GetProperty("id").GetGuid();

        Assert.NotEqual(Guid.Empty, tenantId);


        return tenantId;
    }

    private async Task<HttpResponseMessage> CreateAppUserAsync(
        Guid tenantId,
        string name,
        string username,
        AppUserRole role,
        string? email = null,
        string? phone = null)
    {
        var request = new
        {
            tenantId,
            name,
            username,
            email = email ?? $"{Guid.NewGuid():N}@test.com",
            phone = phone ?? "8095551234",
            role = (int)role
        };

        return await _client.PostAsJsonAsync(
            "/api/app-users",
            request);
    }

    private async Task<AppUserResponse>
        CreateAppUserAndGetAsync(
            Guid tenantId,
            string name,
            string username,
            AppUserRole role)
    {
        var response =
            await CreateAppUserAsync(
                tenantId,
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

    private async Task<CollectionRouteResponse>
        CreateCollectionRouteAsync(
            Guid tenantId)
    {
        var suffix =
            Guid.NewGuid().ToString("N");

        var request = new
        {
            tenantId,
            name = $"Ruta Test {suffix}",
            description = "Ruta creada por AppUsersTests",
            orderMode = (int)CollectionRouteOrderMode.Manual
        };

        var response =
            await _client.PostAsJsonAsync(
                "/api/collection-routes",
                request);

        response.EnsureSuccessStatusCode();

        var route =
            await response.Content
                .ReadFromJsonAsync<CollectionRouteResponse>();

        Assert.NotNull(route);

        return route;
    }

    private static string Unique(
        string prefix)
    {
        return $"{prefix}_{Guid.NewGuid():N}";
    }
}