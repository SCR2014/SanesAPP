using System.Net;
using System.Net.Http.Json;
using Sanes.Api.IntegrationTests.Helpers;
using Sanes.Application.Clients.DTOs;
using Sanes.Application.CollectionRoutes.DTOs;
using Sanes.Application.Investors.DTOs;
using Sanes.Application.Loans.DTOs;
using Sanes.Domain.Enums;

namespace Sanes.Api.IntegrationTests.Loans;

public class LoanCollectionsTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public LoanCollectionsTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetCollections_WithCollectionRoute_ReturnsOnlyLoansFromThatRoute()
    {
        var context = await CreateContextAsync();

        var route1 =
            await CreateRouteAsync(
                context.Client,
                $"Ruta Test A {Guid.NewGuid():N}");

        var route2 =
            await CreateRouteAsync(
                context.Client,
                $"Ruta Test B {Guid.NewGuid():N}");

        var investor =
            await CreateInvestorAsync(
                context.Client);

        var clientRoute1 =
            await CreateClientAsync(
                context.Client,
                route1.Id);

        var clientRoute2 =
            await CreateClientAsync(
                context.Client,
                route2.Id);

        var loanRoute1 =
            await CreateLoanAsync(
                context.Client,
                investor.Id,
                clientRoute1.Id);

        var loanRoute2 =
            await CreateLoanAsync(
                context.Client,
                investor.Id,
                clientRoute2.Id);

        var response =
            await context.Client.GetAsync(
                $"/api/loans/collections" +
                $"?collectionRouteId={route1.Id}");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var portfolio =
            await response.Content
                .ReadFromJsonAsync<
                    List<CollectionLoanItemResponse>>();

        Assert.NotNull(portfolio);

        Assert.Contains(
            portfolio,
            x => x.LoanId == loanRoute1.Id);

        Assert.DoesNotContain(
            portfolio,
            x => x.LoanId == loanRoute2.Id);

        Assert.All(
            portfolio,
            x => Assert.Equal(
                clientRoute1.Id,
                x.ClientId));
    }

    [Fact]
    public async Task GetCollectionsSummary_WithCollectionRoute_CalculatesOnlyThatRoute()
    {
        var context = await CreateContextAsync();

        var route1 =
            await CreateRouteAsync(
                context.Client,
                $"Ruta Summary A {Guid.NewGuid():N}");

        var route2 =
            await CreateRouteAsync(
                context.Client,
                $"Ruta Summary B {Guid.NewGuid():N}");

        var investor =
            await CreateInvestorAsync(
                context.Client);

        var clientRoute1 =
            await CreateClientAsync(
                context.Client,
                route1.Id);

        var clientRoute2 =
            await CreateClientAsync(
                context.Client,
                route2.Id);

        await CreateLoanAsync(
            context.Client,
            investor.Id,
            clientRoute1.Id);

        await CreateLoanAsync(
            context.Client,
            investor.Id,
            clientRoute2.Id);

        var response =
            await context.Client.GetAsync(
                $"/api/loans/collections/summary" +
                $"?collectionRouteId={route1.Id}");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var summary =
            await response.Content
                .ReadFromJsonAsync<
                    CollectionPortfolioSummaryResponse>();

        Assert.NotNull(summary);

        Assert.Equal(
            1,
            summary.LoansCount);

        Assert.Equal(
            1,
            summary.ClientsCount);

        // CreateLoanAsync crea:
        // 13 cuotas x 100 = 1300.
        Assert.Equal(
            1300m,
            summary.TotalBalance);

        Assert.Equal(
            100m,
            summary.TotalNextInstallmentAmountDue);
    }

    [Fact]
    public async Task GetCollections_WithCollectionRouteFromDifferentTenant_ReturnsEmptyList()
    {
        var tenant1 = await CreateContextAsync();
        var tenant2 = await CreateContextAsync();

        var routeFromTenant2 =
            await CreateRouteAsync(
                tenant2.Client,
                $"Ruta Otro Tenant {Guid.NewGuid():N}");

        var response =
            await tenant1.Client.GetAsync(
                $"/api/loans/collections" +
                $"?collectionRouteId={routeFromTenant2.Id}");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var portfolio =
            await response.Content
                .ReadFromJsonAsync<
                    List<CollectionLoanItemResponse>>();

        Assert.NotNull(portfolio);
        Assert.Empty(portfolio);
    }

    [Fact]
    public async Task GetCollections_WithEmptyCollectionRouteId_ReturnsBadRequest()
    {
        var context = await CreateContextAsync();

        var response =
            await context.Client.GetAsync(
                $"/api/loans/collections" +
                $"?collectionRouteId={Guid.Empty}");

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
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

    private static async Task<CollectionRouteResponse>
        CreateRouteAsync(
            HttpClient client,
            string name)
    {
        var request =
            new CreateCollectionRouteRequest
            {
                Name = name,
                Description =
                    "Ruta creada por integration test"
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

    private static async Task<InvestorResponse>
        CreateInvestorAsync(
            HttpClient client)
    {
        var request =
            new CreateInvestorRequest
            {
                Name =
                    $"Investor Test {Guid.NewGuid():N}",
                Phone =
                    "8095551000",
                Identification =
                    $"INV-{Guid.NewGuid():N}"
            };

        var response =
            await client.PostAsJsonAsync(
                "/api/investors",
                request);

        response.EnsureSuccessStatusCode();

        var investor =
            await response.Content
                .ReadFromJsonAsync<
                    InvestorResponse>();

        Assert.NotNull(investor);

        return investor;
    }

    private static async Task<ClientResponse>
        CreateClientAsync(
            HttpClient client,
            Guid collectionRouteId)
    {
        var request =
            new CreateClientRequest
            {
                FirstName =
                    "Cliente",
                LastName =
                    $"Cobranza {Guid.NewGuid():N}",
                Phone =
                    $"809{Random.Shared.Next(
                        1000000,
                        9999999)}",
                CollectionRouteId =
                    collectionRouteId
            };

        var response =
            await client.PostAsJsonAsync(
                "/api/clients",
                request);

        response.EnsureSuccessStatusCode();

        var createdClient =
            await response.Content
                .ReadFromJsonAsync<
                    ClientResponse>();

        Assert.NotNull(createdClient);

        return createdClient;
    }

    private static async Task<LoanResponse>
        CreateLoanAsync(
            HttpClient client,
            Guid investorId,
            Guid clientId)
    {
        var request =
            new CreateLoanRequest
            {
                InvestorId = investorId,
                ClientId = clientId,

                PrincipalAmount = 1000m,
                InstallmentAmount = 100m,
                TotalInstallments = 13,

                PaymentFrequency =
                    PaymentFrequency.Weekly,

                StartDate =
                    DateTime.UtcNow.Date
                        .AddDays(-1),

                Notes =
                    "Loan creado por integration test"
            };

        var response =
            await client.PostAsJsonAsync(
                "/api/loans",
                request);

        response.EnsureSuccessStatusCode();

        var loan =
            await response.Content
                .ReadFromJsonAsync<
                    LoanResponse>();

        Assert.NotNull(loan);

        return loan;
    }
}