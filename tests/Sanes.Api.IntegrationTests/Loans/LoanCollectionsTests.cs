using System.Net;
using System.Net.Http.Json;
using Sanes.Application.Clients.DTOs;
using Sanes.Application.CollectionRoutes.DTOs;
using Sanes.Application.Investors.DTOs;
using Sanes.Application.Loans.DTOs;
using Sanes.Domain.Enums;

using Microsoft.Extensions.DependencyInjection;
using Sanes.Domain.Entities;
using Sanes.Infrastructure.Persistence;

namespace Sanes.Api.IntegrationTests.Loans;

public class LoanCollectionsTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private static readonly Guid TenantId =
        Guid.Parse(
            "8d3fa46b-1553-413d-a30f-60638832a130");

    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public LoanCollectionsTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetCollections_WithCollectionRoute_ReturnsOnlyLoansFromThatRoute()
    {
        // Arrange
        var route1 = await CreateRouteAsync(
            $"Ruta Test A {Guid.NewGuid():N}");

        var route2 = await CreateRouteAsync(
            $"Ruta Test B {Guid.NewGuid():N}");

        var investor =
            await CreateInvestorAsync();

        var clientRoute1 =
            await CreateClientAsync(route1.Id);

        var clientRoute2 =
            await CreateClientAsync(route2.Id);

        var loanRoute1 =
            await CreateLoanAsync(
                investor.Id,
                clientRoute1.Id);

        var loanRoute2 =
            await CreateLoanAsync(
                investor.Id,
                clientRoute2.Id);

        // Act
        var response = await _client.GetAsync(
            $"/api/loans/collections" +
            $"?tenantId={TenantId}" +
            $"&collectionRouteId={route1.Id}");

        // Assert
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
    // Arrange
    var route1 = await CreateRouteAsync(
        $"Ruta Summary A {Guid.NewGuid():N}");

    var route2 = await CreateRouteAsync(
        $"Ruta Summary B {Guid.NewGuid():N}");

    var investor =
        await CreateInvestorAsync();

    var clientRoute1 =
        await CreateClientAsync(route1.Id);

    var clientRoute2 =
        await CreateClientAsync(route2.Id);

    await CreateLoanAsync(
        investor.Id,
        clientRoute1.Id);

    await CreateLoanAsync(
        investor.Id,
        clientRoute2.Id);

    // Act
    var response = await _client.GetAsync(
        $"/api/loans/collections/summary" +
        $"?tenantId={TenantId}" +
        $"&collectionRouteId={route1.Id}");

    // Assert
    Assert.Equal(
        HttpStatusCode.OK,
        response.StatusCode);

    var summary =
        await response.Content
            .ReadFromJsonAsync<
                CollectionPortfolioSummaryResponse>();

    Assert.NotNull(summary);

    Assert.Equal(1, summary.LoansCount);
    Assert.Equal(1, summary.ClientsCount);

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
    // Arrange
    var otherTenantId = Guid.NewGuid();
    var otherRouteId = Guid.NewGuid();

    using (var scope =
        _factory.Services.CreateScope())
    {
        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<SanesDbContext>();

        dbContext.Tenants.Add(
            new Tenant
            {
                Id = otherTenantId,
                Name = $"Tenant Test {Guid.NewGuid():N}",
                CurrencyCode = "DOP",
                CurrencySymbol = "RD$",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

        dbContext.CollectionRoutes.Add(
            new CollectionRoute
            {
                Id = otherRouteId,
                TenantId = otherTenantId,
                Name = $"Ruta Otro Tenant {Guid.NewGuid():N}",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

        await dbContext.SaveChangesAsync();
    }

    // Act
    var response = await _client.GetAsync(
        $"/api/loans/collections" +
        $"?tenantId={TenantId}" +
        $"&collectionRouteId={otherRouteId}");

    // Assert
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
    // Act
    var response = await _client.GetAsync(
        $"/api/loans/collections" +
        $"?tenantId={TenantId}" +
        $"&collectionRouteId={Guid.Empty}");

    // Assert
    Assert.Equal(
        HttpStatusCode.BadRequest,
        response.StatusCode);
}

    private async Task<CollectionRouteResponse> CreateRouteAsync(
        string name)
    {
        var request = new CreateCollectionRouteRequest
        {
            TenantId = TenantId,
            Name = name,
            Description = "Ruta creada por integration test"
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

    private async Task<InvestorResponse> CreateInvestorAsync()
    {
        var request = new CreateInvestorRequest
        {
            TenantId = TenantId,
            Name = $"Investor Test {Guid.NewGuid():N}",
            Phone = "8095551000",
            Identification =
                $"INV-{Guid.NewGuid():N}"
        };

        var response =
            await _client.PostAsJsonAsync(
                "/api/investors",
                request);

        response.EnsureSuccessStatusCode();

        var investor =
            await response.Content
                .ReadFromJsonAsync<InvestorResponse>();

        Assert.NotNull(investor);

        return investor;
    }

    private async Task<ClientResponse> CreateClientAsync(
        Guid collectionRouteId)
    {
        var request = new CreateClientRequest
        {
            TenantId = TenantId,
            FirstName = "Cliente",
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
            await _client.PostAsJsonAsync(
                "/api/clients",
                request);

        response.EnsureSuccessStatusCode();

        var client =
            await response.Content
                .ReadFromJsonAsync<ClientResponse>();

        Assert.NotNull(client);

        return client;
    }

    private async Task<LoanResponse> CreateLoanAsync(
        Guid investorId,
        Guid clientId)
    {
        var request = new CreateLoanRequest
        {
            TenantId = TenantId,
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
            await _client.PostAsJsonAsync(
                "/api/loans",
                request);

        response.EnsureSuccessStatusCode();

        var loan =
            await response.Content
                .ReadFromJsonAsync<LoanResponse>();

        Assert.NotNull(loan);

        return loan;
    }
}