using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Sanes.Api.IntegrationTests.Helpers;
using Sanes.Application.Authentication.DTOs;
using Sanes.Domain.Enums;

namespace Sanes.Api.IntegrationTests.Authorization;

public class AuthorizationTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthorizationTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AdministrativeEndpoint_WithoutToken_ReturnsUnauthorized()
    {
        var client =
            _factory.CreateClient();

        var response =
            await client.GetAsync(
                "/api/clients");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task Collector_AccessingAdministrativeEndpoint_ReturnsForbidden()
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

        var response =
            await collectorClient.GetAsync(
                "/api/clients");

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task Administrator_AccessingFieldCollections_ReturnsForbidden()
    {
        var context =
            await TestAuthenticationHelper
                .ProvisionAdministratorAsync(
                    _factory);

        var response =
            await context.Client.GetAsync(
                "/api/field-collections/daily" +
                "?date=2026-09-14");

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task Collector_AccessingFieldCollections_IsAuthorized()
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

        var response =
            await collectorClient.GetAsync(
                "/api/field-collections/daily" +
                "?date=2026-09-14");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);
    }

    [Fact]
    public async Task Administrator_AccessingAdministrativeEndpoint_IsAuthorized()
    {
        var context =
            await TestAuthenticationHelper
                .ProvisionAdministratorAsync(
                    _factory);

        var response =
            await context.Client.GetAsync(
                "/api/clients");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);
    }

    [Fact]
    public async Task AdministratorToken_CannotAccessOtherTenantResource()
    {
        var tenant1 =
            await TestAuthenticationHelper
                .ProvisionAdministratorAsync(
                    _factory);

        var tenant2 =
            await TestAuthenticationHelper
                .ProvisionAdministratorAsync(
                    _factory);

        var suffix =
            Guid.NewGuid()
                .ToString("N");

        var clientResponse =
            await tenant2.Client.PostAsJsonAsync(
                "/api/clients",
                new
                {
                    firstName =
                        $"Client {suffix[..8]}",

                    lastName =
                        "Tenant Two",

                    phone =
                        "8095551234",

                    identificationType =
                        "Cedula",

                    identification =
                        suffix[..11],

                    address =
                        "Santiago"
                });

        clientResponse.EnsureSuccessStatusCode();

        var client =
            await clientResponse.Content
                .ReadFromJsonAsync<
                    System.Text.Json.JsonElement>();

        var clientId =
            client.GetProperty("id")
                .GetGuid();

        // El JWT es del Tenant 1,
        // pero el recurso pertenece al Tenant 2.
        var response =
            await tenant1.Client.GetAsync(
                $"/api/clients/{clientId}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task InvalidJwt_ReturnsUnauthorized()
    {
        var client =
            _factory.CreateClient();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                "this-is-not-a-valid-jwt");

        var response =
            await client.GetAsync(
                "/api/clients");

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
            $"authorization-collector-{suffix}";

        var password =
            TestAuthenticationHelper
                .DefaultPassword;

        var response =
            await context.Client.PostAsJsonAsync(
                "/api/app-users",
                new
                {
                    name =
                        $"Authorization Collector {suffix}",
                    username,
                    password,
                    email =
                        $"authorization-{suffix}@sanes.test",
                    phone =
                        "8095551234",
                    role =
                        (int)AppUserRole.Collector
                });

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
        var loginClient =
            _factory.CreateClient();

        var response =
            await loginClient.PostAsJsonAsync(
                "/api/auth/login",
                new LoginRequest
                {
                    TenantId =
                        tenantId,

                    Username =
                        collector.Username,

                    Password =
                        collector.Password
                });

        response.EnsureSuccessStatusCode();

        var auth =
            await response.Content
                .ReadFromJsonAsync<
                    AuthResponse>();

        Assert.NotNull(auth);

        return TestAuthenticationHelper
            .CreateAuthenticatedClient(
                _factory,
                auth.AccessToken);
    }

    private sealed record CollectorCredentials(
        Guid Id,
        string Username,
        string Password);
}