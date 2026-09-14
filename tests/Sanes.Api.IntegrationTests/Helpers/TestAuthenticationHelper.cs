using System.Net.Http.Headers;
using System.Net.Http.Json;
using Sanes.Application.Authentication.DTOs;
using Sanes.Application.Provisioning.DTOs;

namespace Sanes.Api.IntegrationTests.Helpers;

public static class TestAuthenticationHelper
{
    public const string DefaultPassword =
        "SanesTest2026!";

    public static async Task<TestTenantContext>
        ProvisionAdministratorAsync(
            CustomWebApplicationFactory factory)
    {
        var client = factory.CreateClient();

        var suffix = Guid.NewGuid()
            .ToString("N");

        var username =
            $"admin_{suffix}";

        var request =
            new ProvisionTenantRequest
            {
                Tenant = new ProvisionTenantData
                {
                    Name =
                        $"Tenant Test {suffix}",
                    LegalName =
                        $"Tenant Test {suffix} SRL",
                    Phone = "8095551234",
                    Email =
                        $"tenant-{suffix}@sanes.test",
                    CurrencyCode = "DOP",
                    CurrencySymbol = "RD$"
                },
                Administrator =
                    new ProvisionAdministratorData
                    {
                        Name =
                            "Administrador Test",
                        Username = username,
                        Password =
                            DefaultPassword,
                        Email =
                            $"admin-{suffix}@sanes.test",
                        Phone = "8095555678"
                    }
            };

        using var message =
            new HttpRequestMessage(
                HttpMethod.Post,
                "/api/provisioning/tenants");

        message.Headers.Add(
            "X-Provisioning-Key",
            CustomWebApplicationFactory
                .TestProvisioningKey);

        message.Content =
            JsonContent.Create(request);

        var provisionResponse =
            await client.SendAsync(message);

        provisionResponse
            .EnsureSuccessStatusCode();

        var provisioned =
            await provisionResponse.Content
                .ReadFromJsonAsync<
                    ProvisionTenantResponse>();

        if (provisioned is null)
        {
            throw new InvalidOperationException(
                "Provisioning response was empty.");
        }

        var loginResponse =
            await client.PostAsJsonAsync(
                "/api/auth/login",
                new LoginRequest
                {
                    TenantId =
                        provisioned.TenantId,
                    Username = username,
                    Password =
                        DefaultPassword
                });

        loginResponse.EnsureSuccessStatusCode();

        var auth =
            await loginResponse.Content
                .ReadFromJsonAsync<AuthResponse>();

        if (auth is null ||
            string.IsNullOrWhiteSpace(
                auth.AccessToken))
        {
            throw new InvalidOperationException(
                "Authentication response did not contain a JWT.");
        }

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                auth.AccessToken);

        return new TestTenantContext
        {
            TenantId =
                provisioned.TenantId,
            AdministratorId =
                provisioned.AdministratorId,
            Username = username,
            Password =
                DefaultPassword,
            AccessToken = auth.AccessToken,
            Client = client
        };
    }

    public static async Task<TestTenantContext>
        CreateAdministratorContextAsync(
            CustomWebApplicationFactory factory)
    {
        return await ProvisionAdministratorAsync(factory);
    }

    public static async Task<string> LoginAsync(
        HttpClient client,
        Guid tenantId,
        string username,
        string password)
    {
        var response =
            await client.PostAsJsonAsync(
                "/api/auth/login",
                new LoginRequest
                {
                    TenantId = tenantId,
                    Username = username,
                    Password = password
                });

        response.EnsureSuccessStatusCode();

        var auth =
            await response.Content
                .ReadFromJsonAsync<AuthResponse>();

        if (auth is null ||
            string.IsNullOrWhiteSpace(
                auth.AccessToken))
        {
            throw new InvalidOperationException(
                "Authentication response did not contain a JWT.");
        }

        return auth.AccessToken;
    }

    public static HttpClient
        CreateAuthenticatedClient(
            CustomWebApplicationFactory factory,
            string token)
    {
        var client =
            factory.CreateClient();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                token);

        return client;
    }
}

public sealed class TestTenantContext
{
    public Guid TenantId { get; init; }

    public Guid AdministratorId { get; init; }

    public string Username { get; init; } =
        string.Empty;

    public string Password { get; init; } =
        string.Empty;

    public string AccessToken { get; init; } =
        string.Empty;

    public HttpClient Client { get; init; } =
        null!;
}