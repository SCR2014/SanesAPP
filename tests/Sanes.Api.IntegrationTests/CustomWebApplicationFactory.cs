using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sanes.Infrastructure.Persistence;

namespace Sanes.Api.IntegrationTests;

public class CustomWebApplicationFactory
    : WebApplicationFactory<Program>
{
    public const string TestProvisioningKey =
        "SanesApp-Test-Provisioning-Key-2026";

    public const string TestJwtKey =
        "SanesApp-Integration-Tests-Jwt-Key-2026-Secure-Only";

    public CustomWebApplicationFactory()
    {
        Environment.SetEnvironmentVariable(
            "Jwt__Issuer",
            "Sanes.Api.Tests");

        Environment.SetEnvironmentVariable(
            "Jwt__Audience",
            "Sanes.Client.Tests");

        Environment.SetEnvironmentVariable(
            "Jwt__ExpirationMinutes",
            "60");

        Environment.SetEnvironmentVariable(
            "Jwt__Key",
            TestJwtKey);

        Environment.SetEnvironmentVariable(
            "Provisioning__Key",
            TestProvisioningKey);
    }

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            var descriptor = services
                .SingleOrDefault(
                    d => d.ServiceType ==
                         typeof(
                             DbContextOptions<
                                 SanesDbContext>));

            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<SanesDbContext>(
                options =>
                {
                    options.UseNpgsql(
                        "Host=localhost;" +
                        "Port=55432;" +
                        "Database=sanesdb_test;" +
                        "Username=sanesadmin;" +
                        "Password=SanesDev2026_Local_ChangeMe!");
                });

            var serviceProvider =
                services.BuildServiceProvider();

            using var scope =
                serviceProvider.CreateScope();

            var dbContext =
                scope.ServiceProvider
                    .GetRequiredService<
                        SanesDbContext>();

            dbContext.Database.Migrate();
        });
    }
}