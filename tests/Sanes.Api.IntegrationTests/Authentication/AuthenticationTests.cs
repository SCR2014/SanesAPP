using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sanes.Api.IntegrationTests.Helpers;
using Sanes.Application.Authentication.DTOs;
using Sanes.Domain.Entities;
using Sanes.Domain.Enums;
using Sanes.Infrastructure.Persistence;

namespace Sanes.Api.IntegrationTests.Authentication;

public class AuthenticationTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthenticationTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_WithValidAdministratorCredentials_ReturnsJwt()
    {
        var context =
            await TestAuthenticationHelper
                .ProvisionAdministratorAsync(
                    _factory);

        var client =
            _factory.CreateClient();

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/login",
                new LoginRequest
                {
                    TenantId = context.TenantId,
                    Username = context.Username,
                    Password = context.Password
                });

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var auth =
            await response.Content
                .ReadFromJsonAsync<AuthResponse>();

        Assert.NotNull(auth);

        Assert.False(
            string.IsNullOrWhiteSpace(
                auth.AccessToken));

        Assert.Equal(
            context.TenantId,
            auth.TenantId);

        Assert.Equal(
            context.AdministratorId,
            auth.AppUserId);

        Assert.Equal(
            context.Username,
            auth.Username);

        Assert.Equal(
            AppUserRole.Administrator,
            auth.Role);

        Assert.True(
            auth.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task Login_WithValidCollectorCredentials_ReturnsJwt()
    {
        var context =
            await TestAuthenticationHelper
                .ProvisionAdministratorAsync(
                    _factory);

        var collector =
            await CreateCollectorAsync(
                context);

        var client =
            _factory.CreateClient();

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/login",
                new LoginRequest
                {
                    TenantId = context.TenantId,
                    Username = collector.Username,
                    Password = collector.Password
                });

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var auth =
            await response.Content
                .ReadFromJsonAsync<AuthResponse>();

        Assert.NotNull(auth);

        Assert.False(
            string.IsNullOrWhiteSpace(
                auth.AccessToken));

        Assert.Equal(
            context.TenantId,
            auth.TenantId);

        Assert.Equal(
            collector.Id,
            auth.AppUserId);

        Assert.Equal(
            collector.Username,
            auth.Username);

        Assert.Equal(
            AppUserRole.Collector,
            auth.Role);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var context =
            await TestAuthenticationHelper
                .ProvisionAdministratorAsync(
                    _factory);

        var client =
            _factory.CreateClient();

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/login",
                new LoginRequest
                {
                    TenantId = context.TenantId,
                    Username = context.Username,
                    Password = "WrongPassword2026!"
                });

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);

        await AssertInvalidCredentialsAsync(
            response);
    }

    [Fact]
    public async Task Login_WithUnknownUsername_ReturnsUnauthorized()
    {
        var context =
            await TestAuthenticationHelper
                .ProvisionAdministratorAsync(
                    _factory);

        var client =
            _factory.CreateClient();

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/login",
                new LoginRequest
                {
                    TenantId = context.TenantId,
                    Username =
                        $"unknown-{Guid.NewGuid():N}",
                    Password =
                        TestAuthenticationHelper
                            .DefaultPassword
                });

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);

        await AssertInvalidCredentialsAsync(
            response);
    }

    [Fact]
    public async Task Login_WithUnknownTenant_ReturnsUnauthorized()
    {
        var context =
            await TestAuthenticationHelper
                .ProvisionAdministratorAsync(
                    _factory);

        var client =
            _factory.CreateClient();

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/login",
                new LoginRequest
                {
                    TenantId = Guid.NewGuid(),
                    Username = context.Username,
                    Password = context.Password
                });

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);

        await AssertInvalidCredentialsAsync(
            response);
    }

    [Fact]
    public async Task Login_WithInactiveUser_ReturnsUnauthorized()
    {
        var context =
            await TestAuthenticationHelper
                .ProvisionAdministratorAsync(
                    _factory);

        var collector =
            await CreateCollectorAsync(
                context);

        var deleteResponse =
            await context.Client.DeleteAsync(
                $"/api/app-users/{collector.Id}");

        Assert.Equal(
            HttpStatusCode.NoContent,
            deleteResponse.StatusCode);

        var client =
            _factory.CreateClient();

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/login",
                new LoginRequest
                {
                    TenantId = context.TenantId,
                    Username = collector.Username,
                    Password = collector.Password
                });

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);

        await AssertInvalidCredentialsAsync(
            response);
    }

    [Fact]
    public async Task Login_WithInactiveTenant_ReturnsUnauthorized()
    {
        var context =
            await TestAuthenticationHelper
                .ProvisionAdministratorAsync(
                    _factory);

        await SetTenantActiveAsync(
            context.TenantId,
            false);

        var client =
            _factory.CreateClient();

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/login",
                new LoginRequest
                {
                    TenantId = context.TenantId,
                    Username = context.Username,
                    Password = context.Password
                });

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);

        await AssertInvalidCredentialsAsync(
            response);
    }

    [Fact]
    public async Task Login_WithNullPasswordHash_ReturnsUnauthorized()
    {
        var context =
            await TestAuthenticationHelper
                .ProvisionAdministratorAsync(
                    _factory);

        var collector =
            await CreateCollectorAsync(
                context);

        await SetPasswordHashAsync(
            collector.Id,
            null);

        var client =
            _factory.CreateClient();

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/login",
                new LoginRequest
                {
                    TenantId = context.TenantId,
                    Username = collector.Username,
                    Password = collector.Password
                });

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);

        await AssertInvalidCredentialsAsync(
            response);
    }

    [Fact]
    public async Task ExistingJwt_AfterUserDeactivation_ReturnsUnauthorized()
    {
        var context =
            await TestAuthenticationHelper
                .ProvisionAdministratorAsync(
                    _factory);

        var collector =
            await CreateCollectorAsync(
                context);

        var collectorClient =
            await LoginCollectorAsync(
                context.TenantId,
                collector);

        // Confirma primero que el JWT funciona.
        var beforeDeactivation =
            await collectorClient.GetAsync(
                "/api/field-collections/daily" +
                "?date=2026-09-14");

        Assert.Equal(
            HttpStatusCode.OK,
            beforeDeactivation.StatusCode);

        var deleteResponse =
            await context.Client.DeleteAsync(
                $"/api/app-users/{collector.Id}");

        Assert.Equal(
            HttpStatusCode.NoContent,
            deleteResponse.StatusCode);

        // Reutilizamos exactamente el mismo JWT.
        var afterDeactivation =
            await collectorClient.GetAsync(
                "/api/field-collections/daily" +
                "?date=2026-09-14");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            afterDeactivation.StatusCode);
    }

    [Fact]
    public async Task ExistingJwt_AfterTenantDeactivation_ReturnsUnauthorized()
    {
        var context =
            await TestAuthenticationHelper
                .ProvisionAdministratorAsync(
                    _factory);

        var beforeDeactivation =
            await context.Client.GetAsync(
                "/api/tenants/me");

        Assert.Equal(
            HttpStatusCode.OK,
            beforeDeactivation.StatusCode);

        await SetTenantActiveAsync(
            context.TenantId,
            false);

        // Reutilizamos el JWT emitido antes de desactivar
        // el tenant.
        var afterDeactivation =
            await context.Client.GetAsync(
                "/api/tenants/me");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            afterDeactivation.StatusCode);
    }

    [Fact]
    public async Task
        ExistingJwt_AfterUserRoleChange_ReturnsUnauthorized()
    {
        var context =
            await TestAuthenticationHelper
                .ProvisionAdministratorAsync(
                    _factory);

        // Confirmamos primero que el JWT actual funciona.
        var initialResponse =
            await context.Client.GetAsync(
                "/api/tenants/me");

        Assert.Equal(
            HttpStatusCode.OK,
            initialResponse.StatusCode);

        // Cambiamos directamente el rol en la BD.
        // Lo hacemos así para probar específicamente que
        // OnTokenValidated detecta que el rol del JWT ya no
        // coincide con el rol actual del usuario.
        using (var scope =
            _factory.Services.CreateScope())
        {
            var dbContext =
                scope.ServiceProvider
                    .GetRequiredService<SanesDbContext>();

            var appUser =
                await dbContext.AppUsers
                    .SingleAsync(
                        x =>
                            x.Id ==
                            context.AdministratorId &&
                            x.TenantId ==
                            context.TenantId);

            appUser.Role =
                AppUserRole.Collector;

            appUser.UpdatedAt =
                DateTime.UtcNow;

            await dbContext.SaveChangesAsync();
        }

        // Utilizamos exactamente el mismo JWT emitido
        // cuando el usuario era Administrator.
        var response =
            await context.Client.GetAsync(
                "/api/tenants/me");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    // ============================================================
    // HELPERS
    // ============================================================

    private async Task<CollectorCredentials>
        CreateCollectorAsync(
            TestTenantContext context)
    {
        var suffix =
            Guid.NewGuid()
                .ToString("N");

        var username =
            $"auth-collector-{suffix}";

        var password =
            TestAuthenticationHelper
                .DefaultPassword;

        var request = new
        {
            name =
                $"Auth Collector {suffix}",
            username,
            password,
            email =
                $"auth-collector-{suffix}@sanes.test",
            phone =
                "8095551234",
            role =
                (int)AppUserRole.Collector
        };

        var response =
            await context.Client.PostAsJsonAsync(
                "/api/app-users",
                request);

        response.EnsureSuccessStatusCode();

        var json =
            await response.Content
                .ReadFromJsonAsync<
                    System.Text.Json.JsonElement>();

        return new CollectorCredentials(
            json.GetProperty("id")
                .GetGuid(),
            username,
            password);
    }

    private async Task<HttpClient>
        LoginCollectorAsync(
            Guid tenantId,
            CollectorCredentials collector)
    {
        var client =
            _factory.CreateClient();

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/login",
                new LoginRequest
                {
                    TenantId = tenantId,
                    Username =
                        collector.Username,
                    Password =
                        collector.Password
                });

        response.EnsureSuccessStatusCode();

        var auth =
            await response.Content
                .ReadFromJsonAsync<AuthResponse>();

        Assert.NotNull(auth);

        Assert.False(
            string.IsNullOrWhiteSpace(
                auth.AccessToken));

        return TestAuthenticationHelper
            .CreateAuthenticatedClient(
                _factory,
                auth.AccessToken);
    }

    private async Task SetTenantActiveAsync(
        Guid tenantId,
        bool isActive)
    {
        using var scope =
            _factory.Services
                .CreateScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<
                    SanesDbContext>();

        var tenant =
            await dbContext.Tenants
                .SingleAsync(
                    x => x.Id == tenantId);

        tenant.IsActive =
            isActive;

        tenant.UpdatedAt =
            DateTime.UtcNow;

        await dbContext
            .SaveChangesAsync();
    }

    private async Task SetPasswordHashAsync(
        Guid appUserId,
        string? passwordHash)
    {
        using var scope =
            _factory.Services
                .CreateScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<
                    SanesDbContext>();

        var appUser =
            await dbContext.AppUsers
                .SingleAsync(
                    x => x.Id == appUserId);

        appUser.PasswordHash =
            passwordHash;

        appUser.UpdatedAt =
            DateTime.UtcNow;

        await dbContext
            .SaveChangesAsync();
    }

    private static async Task
        AssertInvalidCredentialsAsync(
            HttpResponseMessage response)
    {
        var json =
            await response.Content
                .ReadFromJsonAsync<
                    System.Text.Json.JsonElement>();

        Assert.True(
            json.TryGetProperty(
                "message",
                out var message));

        Assert.Equal(
            "Invalid credentials.",
            message.GetString());
    }

    private sealed record CollectorCredentials(
        Guid Id,
        string Username,
        string Password);
}