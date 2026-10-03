using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Sanes.Api.IntegrationTests;

public class HealthChecksTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory
        _factory;

    public HealthChecksTests(
        CustomWebApplicationFactory factory)
    {
        _factory =
            factory;
    }

    [Fact]
    public async Task Live_WithoutAuthentication_ReturnsOk()
    {
        using var client =
            _factory.CreateClient();

        using var response =
            await client.GetAsync(
                "/health/live");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);
    }

    [Fact]
    public async Task Ready_WithPostgreSqlAvailable_ReturnsOk()
    {
        using var client =
            _factory.CreateClient();

        using var response =
            await client.GetAsync(
                "/health/ready");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);
    }

    [Fact]
    public async Task Live_WithPostgreSqlUnavailable_ReturnsOk()
    {
        await using var factory =
            new UnavailableDatabaseWebApplicationFactory();

        using var client =
            factory.CreateClient();

        using var response =
            await client.GetAsync(
                "/health/live");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);
    }

    [Fact]
    public async Task Ready_WithPostgreSqlUnavailable_ReturnsServiceUnavailable()
    {
        await using var factory =
            new UnavailableDatabaseWebApplicationFactory();

        using var client =
            factory.CreateClient();

        using var response =
            await client.GetAsync(
                "/health/ready");

        Assert.Equal(
            HttpStatusCode.ServiceUnavailable,
            response.StatusCode);
    }

    private sealed class UnavailableDatabaseWebApplicationFactory
        : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(
            IWebHostBuilder builder)
        {
            builder.UseEnvironment(
                "Testing");

            builder.ConfigureAppConfiguration(
                (_, configurationBuilder) =>
                {
                    configurationBuilder.AddInMemoryCollection(
                        new Dictionary<string, string?>
                        {
                            ["Jwt:Issuer"] =
                                "Sanes.Api.Tests",

                            ["Jwt:Audience"] =
                                "Sanes.Client.Tests",

                            ["Jwt:ExpirationMinutes"] =
                                "60",

                            ["Jwt:Key"] =
                                CustomWebApplicationFactory
                                    .TestJwtKey,

                            ["Provisioning:Key"] =
                                CustomWebApplicationFactory
                                    .TestProvisioningKey,

                            ["ConnectionStrings:DefaultConnection"] =
                                "Host=127.0.0.1;" +
                                "Port=1;" +
                                "Database=sanesdb_unavailable;" +
                                "Username=sanesadmin;" +
                                "Password=unused;" +
                                "Timeout=1;" +
                                "Command Timeout=1",

                            ["FileStorage:Provider"] =
                                "Local",

                            ["FileStorage:RootPath"] =
                                Path.Combine(
                                    Path.GetTempPath(),
                                    "SanesApp.HealthChecks",
                                    Guid.NewGuid().ToString("N"))
                        });
                });
        }
    }
}
