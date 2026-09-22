using System.Net;
using System.Net.Http.Json;
using Sanes.Api.IntegrationTests.Helpers;
using Sanes.Application.Investors.DTOs;

namespace Sanes.Api.IntegrationTests;

public sealed class InvestorsTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory
        _factory;

    public InvestorsTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAll_WithoutIncludeInactive_ReturnsOnlyActiveInvestors()
    {
        var context =
            await TestAuthenticationHelper
                .ProvisionAdministratorAsync(
                    _factory);

        var active =
            await CreateInvestorAsync(
                context.Client,
                "Activo");

        var inactive =
            await CreateInvestorAsync(
                context.Client,
                "Inactivo");

        var deleteResponse =
            await context.Client.DeleteAsync(
                $"/api/investors/{inactive.Id:D}");

        Assert.Equal(
            HttpStatusCode.NoContent,
            deleteResponse.StatusCode);

        var response =
            await context.Client.GetAsync(
                "/api/investors");

        response.EnsureSuccessStatusCode();

        var investors =
            await response.Content
                .ReadFromJsonAsync<
                    List<InvestorResponse>>();

        Assert.NotNull(investors);

        Assert.Contains(
            investors!,
            x =>
                x.Id == active.Id);

        Assert.DoesNotContain(
            investors!,
            x =>
                x.Id == inactive.Id);

        Assert.All(
            investors!,
            x =>
                Assert.True(
                    x.IsActive));
    }

    [Fact]
    public async Task GetAll_WithIncludeInactive_ReturnsActiveAndInactiveInvestors()
    {
        var context =
            await TestAuthenticationHelper
                .ProvisionAdministratorAsync(
                    _factory);

        var active =
            await CreateInvestorAsync(
                context.Client,
                "Activo");

        var inactive =
            await CreateInvestorAsync(
                context.Client,
                "Inactivo");

        var deleteResponse =
            await context.Client.DeleteAsync(
                $"/api/investors/{inactive.Id:D}");

        Assert.Equal(
            HttpStatusCode.NoContent,
            deleteResponse.StatusCode);

        var response =
            await context.Client.GetAsync(
                "/api/investors?includeInactive=true");

        response.EnsureSuccessStatusCode();

        var investors =
            await response.Content
                .ReadFromJsonAsync<
                    List<InvestorResponse>>();

        Assert.NotNull(investors);

        var activeResult =
            Assert.Single(
                investors!,
                x =>
                    x.Id == active.Id);

        var inactiveResult =
            Assert.Single(
                investors!,
                x =>
                    x.Id == inactive.Id);

        Assert.True(
            activeResult.IsActive);

        Assert.False(
            inactiveResult.IsActive);
    }

    [Fact]
    public async Task GetAll_WithIncludeInactive_DoesNotLeakOtherTenantInvestors()
    {
        var tenantA =
            await TestAuthenticationHelper
                .ProvisionAdministratorAsync(
                    _factory);

        var tenantB =
            await TestAuthenticationHelper
                .ProvisionAdministratorAsync(
                    _factory);

        var investorA =
            await CreateInvestorAsync(
                tenantA.Client,
                "Tenant A");

        var investorB =
            await CreateInvestorAsync(
                tenantB.Client,
                "Tenant B");

        var response =
            await tenantA.Client.GetAsync(
                "/api/investors?includeInactive=true");

        response.EnsureSuccessStatusCode();

        var investors =
            await response.Content
                .ReadFromJsonAsync<
                    List<InvestorResponse>>();

        Assert.NotNull(investors);

        Assert.Contains(
            investors!,
            x =>
                x.Id == investorA.Id);

        Assert.DoesNotContain(
            investors!,
            x =>
                x.Id == investorB.Id);

        Assert.All(
            investors!,
            x =>
                Assert.Equal(
                    tenantA.TenantId,
                    x.TenantId));
    }

    private static async Task<InvestorResponse>
        CreateInvestorAsync(
            HttpClient client,
            string description)
    {
        var suffix =
            Guid.NewGuid()
                .ToString("N");

        var response =
            await client.PostAsJsonAsync(
                "/api/investors",
                new CreateInvestorRequest
                {
                    Name =
                        $"Investor {description} {suffix[..8]}",
                    Phone =
                        "8095551234",
                    Email =
                        $"investor-{suffix}@sanes.test",
                    Identification =
                        suffix,
                    Notes =
                        $"Investor de integración {description}"
                });

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var investor =
            await response.Content
                .ReadFromJsonAsync<
                    InvestorResponse>();

        Assert.NotNull(
            investor);

        return investor!;
    }
}