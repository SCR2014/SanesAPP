using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sanes.Application.Authentication.DTOs;
using Sanes.Application.Provisioning.DTOs;
using Sanes.Domain.Enums;
using Sanes.Infrastructure.Persistence;

namespace Sanes.Api.IntegrationTests.Provisioning;

public class ProvisioningTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ProvisioningTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ProvisionTenant_WithoutProvisioningKey_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var request = CreateProvisionRequest();

        var response =
            await client.PostAsJsonAsync(
                "/api/provisioning/tenants",
                request);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task ProvisionTenant_WithInvalidProvisioningKey_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        using var message =
            new HttpRequestMessage(
                HttpMethod.Post,
                "/api/provisioning/tenants");

        message.Headers.Add(
            "X-Provisioning-Key",
            "Invalid-Provisioning-Key");

        message.Content =
            JsonContent.Create(
                CreateProvisionRequest());

        var response =
            await client.SendAsync(message);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task ProvisionTenant_WithValidKey_CreatesTenantAndAdministrator()
    {
        var request =
            CreateProvisionRequest();

        var response =
            await SendProvisioningRequestAsync(
                request);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    ProvisionTenantResponse>();

        Assert.NotNull(result);

        Assert.NotEqual(
            Guid.Empty,
            result.TenantId);

        Assert.NotEqual(
            Guid.Empty,
            result.AdministratorId);

        await AssertTenantExistsAsync(
            result.TenantId,
            request.Tenant.Name);

        await AssertAdministratorExistsAsync(
            result.AdministratorId,
            result.TenantId,
            request.Administrator.Username);
    }

    [Fact]
    public async Task ProvisionTenant_CreatesFirstUserAsAdministrator()
    {
        var request =
            CreateProvisionRequest();

        var response =
            await SendProvisioningRequestAsync(
                request);

        response.EnsureSuccessStatusCode();

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    ProvisionTenantResponse>();

        Assert.NotNull(result);

        using var scope =
            _factory.Services
                .CreateScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<
                    SanesDbContext>();

        var appUser =
            await dbContext.AppUsers
                .AsNoTracking()
                .SingleAsync(
                    x =>
                        x.Id ==
                        result.AdministratorId);

        Assert.Equal(
            AppUserRole.Administrator,
            appUser.Role);

        Assert.True(
            appUser.IsActive);
    }

    [Fact]
    public async Task ProvisionTenant_ResponseDoesNotExposePasswordOrPasswordHash()
    {
        var response =
            await SendProvisioningRequestAsync(
                CreateProvisionRequest());

        response.EnsureSuccessStatusCode();

        var content =
            await response.Content
                .ReadAsStringAsync();

        Assert.DoesNotContain(
            "passwordHash",
            content,
            StringComparison.OrdinalIgnoreCase);

        Assert.DoesNotContain(
            "password",
            content,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProvisionTenant_CredentialsCanLogin()
    {
        var request =
            CreateProvisionRequest();

        var provisionResponse =
            await SendProvisioningRequestAsync(
                request);

        provisionResponse
            .EnsureSuccessStatusCode();

        var provisioned =
            await provisionResponse.Content
                .ReadFromJsonAsync<
                    ProvisionTenantResponse>();

        Assert.NotNull(provisioned);

        var client =
            _factory.CreateClient();

        var loginResponse =
            await client.PostAsJsonAsync(
                "/api/auth/login",
                new LoginRequest
                {
                    TenantId =
                        provisioned.TenantId,

                    Username =
                        request.Administrator
                            .Username,

                    Password =
                        request.Administrator
                            .Password
                });

        Assert.Equal(
            HttpStatusCode.OK,
            loginResponse.StatusCode);

        var auth =
            await loginResponse.Content
                .ReadFromJsonAsync<
                    AuthResponse>();

        Assert.NotNull(auth);

        Assert.False(
            string.IsNullOrWhiteSpace(
                auth.AccessToken));

        Assert.Equal(
            provisioned.TenantId,
            auth.TenantId);

        Assert.Equal(
            provisioned.AdministratorId,
            auth.AppUserId);

        Assert.Equal(
            AppUserRole.Administrator,
            auth.Role);
    }

    [Fact]
    public async Task ProvisionTenant_PersistsPasswordAsHash()
    {
        var request =
            CreateProvisionRequest();

        var response =
            await SendProvisioningRequestAsync(
                request);

        response.EnsureSuccessStatusCode();

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    ProvisionTenantResponse>();

        Assert.NotNull(result);

        using var scope =
            _factory.Services
                .CreateScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<
                    SanesDbContext>();

        var appUser =
            await dbContext.AppUsers
                .AsNoTracking()
                .SingleAsync(
                    x =>
                        x.Id ==
                        result.AdministratorId);

        Assert.False(
            string.IsNullOrWhiteSpace(
                appUser.PasswordHash));

        Assert.NotEqual(
            request.Administrator.Password,
            appUser.PasswordHash);
    }

    // ============================================================
    // HELPERS
    // ============================================================

    private ProvisionTenantRequest
        CreateProvisionRequest()
    {
        var suffix =
            Guid.NewGuid()
                .ToString("N");

        return new ProvisionTenantRequest
        {
            Tenant =
                new ProvisionTenantData
                {
                    Name =
                        $"Provisioning Tenant {suffix}",

                    LegalName =
                        $"Provisioning Tenant {suffix} SRL",

                    Phone =
                        "8095551234",

                    Email =
                        $"tenant-{suffix}@sanes.test",

                    CurrencyCode =
                        "DOP",

                    CurrencySymbol =
                        "RD$"
                },

            Administrator =
                new ProvisionAdministratorData
                {
                    Name =
                        "Provisioning Administrator",

                    Username =
                        $"provision-admin-{suffix}",

                    Password =
                        "ProvisionTest2026!",

                    Email =
                        $"admin-{suffix}@sanes.test",

                    Phone =
                        "8095555678"
                }
        };
    }

    private async Task<HttpResponseMessage>
        SendProvisioningRequestAsync(
            ProvisionTenantRequest request)
    {
        var client =
            _factory.CreateClient();

        using var message =
            new HttpRequestMessage(
                HttpMethod.Post,
                "/api/provisioning/tenants");

        message.Headers.Add(
            "X-Provisioning-Key",
            CustomWebApplicationFactory
                .TestProvisioningKey);

        message.Content =
            JsonContent.Create(
                request);

        return await client.SendAsync(
            message);
    }

    private async Task
        AssertTenantExistsAsync(
            Guid tenantId,
            string expectedName)
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
                .AsNoTracking()
                .SingleAsync(
                    x =>
                        x.Id ==
                        tenantId);

        Assert.Equal(
            expectedName,
            tenant.Name);

        Assert.True(
            tenant.IsActive);
    }

    private async Task
        AssertAdministratorExistsAsync(
            Guid administratorId,
            Guid tenantId,
            string expectedUsername)
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
                .AsNoTracking()
                .SingleAsync(
                    x =>
                        x.Id ==
                        administratorId);

        Assert.Equal(
            tenantId,
            appUser.TenantId);

        Assert.Equal(
            expectedUsername,
            appUser.Username);

        Assert.Equal(
            AppUserRole.Administrator,
            appUser.Role);

        Assert.True(
            appUser.IsActive);

        Assert.False(
            string.IsNullOrWhiteSpace(
                appUser.PasswordHash));
    }
}