using Microsoft.AspNetCore.Mvc.Testing;

namespace Sanes.Api.IntegrationTests;

[Collection("StartupConfiguration")]
public class StartupConfigurationTests
{
    [Fact]
    public void Staging_WithoutDefaultConnection_FailsFast()
    {
        using var environment =
            new EnvironmentVariableScope(
                new Dictionary<string, string?>
                {
                    ["ASPNETCORE_ENVIRONMENT"] =
                        "Staging",

                    ["Jwt__Issuer"] =
                        "Sanes.Api.Tests",

                    ["Jwt__Audience"] =
                        "Sanes.Client.Tests",

                    ["Jwt__ExpirationMinutes"] =
                        "60",

                    ["Jwt__Key"] =
                        CustomWebApplicationFactory
                            .TestJwtKey,

                    ["ConnectionStrings__DefaultConnection"] =
                        null
                });

        using var factory =
            new WebApplicationFactory<Program>();

        var exception =
            Assert.Throws<InvalidOperationException>(
                () =>
                {
                    _ = factory.Services;
                });

        Assert.Equal(
            "ConnectionStrings:DefaultConnection is not configured.",
            exception.Message);
    }

    [Fact]
    public void Staging_WithInvalidJwtExpiration_FailsFast()
    {
        using var environment =
            new EnvironmentVariableScope(
                new Dictionary<string, string?>
                {
                    ["ASPNETCORE_ENVIRONMENT"] =
                        "Staging",

                    ["Jwt__Issuer"] =
                        "Sanes.Api.Tests",

                    ["Jwt__Audience"] =
                        "Sanes.Client.Tests",

                    ["Jwt__ExpirationMinutes"] =
                        "0",

                    ["Jwt__Key"] =
                        CustomWebApplicationFactory
                            .TestJwtKey,

                    ["ConnectionStrings__DefaultConnection"] =
                        "Host=localhost;" +
                        "Port=5432;" +
                        "Database=sanesdb;" +
                        "Username=sanesadmin;" +
                        "Password=unused"
                });

        using var factory =
            new WebApplicationFactory<Program>();

        var exception =
            Assert.Throws<InvalidOperationException>(
                () =>
                {
                    _ = factory.Services;
                });

        Assert.Equal(
            "JWT expiration must be greater than zero.",
            exception.Message);
    }

    private sealed class EnvironmentVariableScope
        : IDisposable
    {
        private readonly Dictionary<
            string,
            string?> _originalValues =
                new();

        public EnvironmentVariableScope(
            IReadOnlyDictionary<
                string,
                string?> values)
        {
            foreach (var pair in values)
            {
                _originalValues[pair.Key] =
                    Environment.GetEnvironmentVariable(
                        pair.Key);

                Environment.SetEnvironmentVariable(
                    pair.Key,
                    pair.Value);
            }
        }

        public void Dispose()
        {
            foreach (var pair in _originalValues)
            {
                Environment.SetEnvironmentVariable(
                    pair.Key,
                    pair.Value);
            }
        }
    }
}
