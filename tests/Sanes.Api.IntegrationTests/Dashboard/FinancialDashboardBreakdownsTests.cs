using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sanes.Api.IntegrationTests.Helpers;
using Sanes.Application.Authentication.DTOs;
using Sanes.Application.Clients.DTOs;
using Sanes.Application.Dashboard.DTOs;
using Sanes.Application.Investors.DTOs;
using Sanes.Application.Loans.DTOs;
using Sanes.Application.Payments.DTOs;
using Sanes.Domain.Entities;
using Sanes.Domain.Enums;
using Sanes.Infrastructure.Persistence;

namespace Sanes.Api.IntegrationTests.Dashboard;

public class FinancialDashboardBreakdownsTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public FinancialDashboardBreakdownsTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ============================================================
    // INVESTORS
    // ============================================================

    [Fact]
    public async Task Investors_TwoInvestors_KeepFinancialMetricsSeparated()
    {
        var context =
            await CreateContextAsync();

        var investorA =
            await CreateInvestorAsync(
                context.Client,
                "Investor A");

        var investorB =
            await CreateInvestorAsync(
                context.Client,
                "Investor B");

        var clientA =
            await CreateClientAsync(
                context.Client);

        var clientB =
            await CreateClientAsync(
                context.Client);

        var loanA =
            await CreateLoanAsync(
                context.Client,
                investorA.Id,
                clientA.Id,
                DateTime.UtcNow.Date);

        var loanB =
            await CreateLoanAsync(
                context.Client,
                investorB.Id,
                clientB.Id,
                DateTime.UtcNow.Date);

        var paymentResponse =
            await CreatePaymentAsync(
                context.Client,
                loanA.Id,
                100m,
                DateTime.UtcNow,
                PaymentType.Regular);

        Assert.Equal(
            HttpStatusCode.Created,
            paymentResponse.StatusCode);

        var response =
            await context.Client.GetAsync(
                "/api/dashboard/investors");

        response.EnsureSuccessStatusCode();

        var result =
            await response.Content
                .ReadFromJsonAsync<List<
                    FinancialDashboardInvestorBreakdownResponse>>();

        Assert.NotNull(result);

        var itemA =
            Assert.Single(
                result, x =>
                    x.InvestorId == investorA.Id);

        var itemB =
            Assert.Single(
                result, x =>
                    x.InvestorId == investorB.Id);

        Assert.Equal(1, itemA.ActiveLoansCount);
        Assert.Equal(1, itemA.ActiveClientsCount);
        Assert.Equal(1000m, itemA.PrincipalOriginated);
        Assert.Equal(1300m, itemA.ContractualAmountOriginated);
        Assert.Equal(300m, itemA.GrossContractualInterest);
        Assert.Equal(300m, itemA.NetContractualInterest);

        Assert.Equal(
            1200m,
            itemA.ContractualBalanceOutstanding);

        Assert.Equal(
            100m,
            itemA.CashCollected);

        Assert.Equal(
            100m,
            itemA.ContractualCashCollected);

        Assert.Equal(
            0m,
            itemA.LateFeesCollected);

        Assert.Equal(1, itemB.ActiveLoansCount);
        Assert.Equal(1, itemB.ActiveClientsCount);
        Assert.Equal(1000m, itemB.PrincipalOriginated);
        Assert.Equal(1300m, itemB.ContractualAmountOriginated);

        Assert.Equal(
            1300m,
            itemB.ContractualBalanceOutstanding);

        Assert.Equal(
            0m,
            itemB.CashCollected);

        Assert.Equal(
            0m,
            itemB.ContractualCashCollected);

        Assert.NotEqual(
            loanA.Id,
            loanB.Id);
    }

    [Fact]
    public async Task Investors_EarlySettlementDiscount_IsAttributedToCorrectInvestor()
    {
        var context =
            await CreateContextAsync();

        var investor =
            await CreateInvestorAsync(
                context.Client,
                "Settlement Investor");

        var otherInvestor =
            await CreateInvestorAsync(
                context.Client,
                "Other Investor");

        var client =
            await CreateClientAsync(
                context.Client);

        var loan =
            await CreateLoanAsync(
                context.Client,
                investor.Id,
                client.Id,
                DateTime.UtcNow.Date);

        for (var i = 0; i < 6; i++)
        {
            var paymentResponse =
                await CreatePaymentAsync(
                    context.Client,
                    loan.Id,
                    100m,
                    DateTime.UtcNow,
                    PaymentType.Regular);

            Assert.Equal(
                HttpStatusCode.Created,
                paymentResponse.StatusCode);
        }

        var quote =
            await QuoteAsync(
                context.Client,
                loan.Id,
                EarlySettlementDiscountType
                    .InstallmentWaiver,
                2m);

        Assert.Equal(
            700m,
            quote.ContractualBalance);

        Assert.Equal(
            200m,
            quote.DiscountAmount);

        Assert.Equal(
            500m,
            quote.SettlementAmount);

        await ExecuteAsync(
            context.Client,
            loan.Id,
            EarlySettlementDiscountType
                .InstallmentWaiver,
            2m,
            quote.SettlementAmount);

        var response =
            await context.Client.GetAsync(
                "/api/dashboard/investors");

        response.EnsureSuccessStatusCode();

        var result =
            await response.Content
                .ReadFromJsonAsync<List<
                    FinancialDashboardInvestorBreakdownResponse>>();

        Assert.NotNull(result);

        var item =
            Assert.Single(
                result, x =>
                    x.InvestorId == investor.Id);

        Assert.Equal(
            0,
            item.ActiveLoansCount);

        Assert.Equal(
            1000m,
            item.PrincipalOriginated);

        Assert.Equal(
            1300m,
            item.ContractualAmountOriginated);

        Assert.Equal(
            300m,
            item.GrossContractualInterest);

        Assert.Equal(
            200m,
            item.EarlySettlementDiscounts);

        Assert.Equal(
            100m,
            item.NetContractualInterest);

        Assert.Equal(
            1100m,
            item.CashCollected);

        Assert.Equal(
            1100m,
            item.ContractualCashCollected);

        Assert.Equal(
            0m,
            item.ContractualBalanceOutstanding);

        var other =
            Assert.Single(
                result, x =>
                    x.InvestorId ==
                    otherInvestor.Id);

        Assert.Equal(
            0m,
            other.EarlySettlementDiscounts);

        Assert.Equal(
            0m,
            other.CashCollected);
    }

    [Fact]
    public async Task Investors_ActiveInvestorWithoutLoans_AppearsWithZeros()
    {
        var context =
            await CreateContextAsync();

        var investor =
            await CreateInvestorAsync(
                context.Client,
                "Empty Active Investor");

        var response =
            await context.Client.GetAsync(
                "/api/dashboard/investors");

        response.EnsureSuccessStatusCode();

        var result =
            await response.Content
                .ReadFromJsonAsync<List<
                    FinancialDashboardInvestorBreakdownResponse>>();

        Assert.NotNull(result);

        var item =
            Assert.Single(
                result, x =>
                    x.InvestorId == investor.Id);

        Assert.True(
            item.IsActive);

        Assert.Equal(
            0,
            item.ActiveLoansCount);

        Assert.Equal(
            0,
            item.ActiveClientsCount);

        Assert.Equal(
            0m,
            item.PrincipalOriginated);

        Assert.Equal(
            0m,
            item.ContractualBalanceOutstanding);

        Assert.Equal(
            0m,
            item.CashCollected);
    }

    [Fact]
    public async Task Investors_InactiveInvestorWithoutActivity_IsHidden()
    {
        var context =
            await CreateContextAsync();

        var investor =
            await CreateInvestorAsync(
                context.Client,
                "Inactive Empty Investor");

        await SetInvestorActiveAsync(
            context.TenantId,
            investor.Id,
            false);

        var response =
            await context.Client.GetAsync(
                "/api/dashboard/investors");

        response.EnsureSuccessStatusCode();

        var result =
            await response.Content
                .ReadFromJsonAsync<List<
                    FinancialDashboardInvestorBreakdownResponse>>();

        Assert.NotNull(result);

        Assert.DoesNotContain(
            result,
            x =>
                x.InvestorId == investor.Id);
    }

    [Fact]
    public async Task Investors_AreTenantIsolated()
    {
        var tenantOne =
            await CreateContextAsync();

        var tenantTwo =
            await CreateContextAsync();

        var investor =
            await CreateInvestorAsync(
                tenantTwo.Client,
                "Tenant Two Investor");

        var client =
            await CreateClientAsync(
                tenantTwo.Client);

        await CreateLoanAsync(
            tenantTwo.Client,
            investor.Id,
            client.Id,
            DateTime.UtcNow.Date);

        var response =
            await tenantOne.Client.GetAsync(
                "/api/dashboard/investors");

        response.EnsureSuccessStatusCode();

        var result =
            await response.Content
                .ReadFromJsonAsync<List<
                    FinancialDashboardInvestorBreakdownResponse>>();

        Assert.NotNull(result);

        Assert.DoesNotContain(
            result,
            x =>
                x.InvestorId == investor.Id);
    }

    // ============================================================
    // ROUTES
    // ============================================================

    [Fact]
    public async Task Routes_GroupsCurrentPortfolioByRoute()
    {
        var context =
            await CreateContextAsync();

        var routeA =
            await CreateRouteAsync(
                context.TenantId,
                "Ruta A");

        var routeB =
            await CreateRouteAsync(
                context.TenantId,
                "Ruta B");

        var investor =
            await CreateInvestorAsync(
                context.Client,
                "Route Investor");

        var clientA =
            await CreateClientAsync(
                context.Client,
                routeA.Id);

        var clientB =
            await CreateClientAsync(
                context.Client,
                routeB.Id);

        await CreateLoanAsync(
            context.Client,
            investor.Id,
            clientA.Id,
            DateTime.UtcNow.Date);

        await CreateLoanAsync(
            context.Client,
            investor.Id,
            clientB.Id,
            DateTime.UtcNow.Date);

        var response =
            await context.Client.GetAsync(
                "/api/dashboard/routes");

        response.EnsureSuccessStatusCode();

        var result =
            await response.Content
                .ReadFromJsonAsync<List<
                    FinancialDashboardRouteBreakdownResponse>>();

        Assert.NotNull(result);

        var itemA =
            Assert.Single(
                result, x =>
                    x.CollectionRouteId ==
                    routeA.Id);

        var itemB =
            Assert.Single(
                result, x =>
                    x.CollectionRouteId ==
                    routeB.Id);

        Assert.Equal(
            1,
            itemA.ActiveLoansCount);

        Assert.Equal(
            1,
            itemA.ActiveClientsCount);

        Assert.Equal(
            1300m,
            itemA.ContractualBalanceOutstanding);

        Assert.Equal(
            1300m,
            itemA.TotalOutstanding);

        Assert.Equal(
            100m,
            itemA.NextInstallmentAmountDue);

        Assert.Equal(
            100m,
            itemA.CollectionAmountDue);

        Assert.Equal(
            1,
            itemB.ActiveLoansCount);

        Assert.Equal(
            1300m,
            itemB.ContractualBalanceOutstanding);
    }

    [Fact]
    public async Task Routes_ActiveRouteWithoutPortfolio_AppearsWithZeros()
    {
        var context =
            await CreateContextAsync();

        var route =
            await CreateRouteAsync(
                context.TenantId,
                "Ruta Vacía");

        var response =
            await context.Client.GetAsync(
                "/api/dashboard/routes");

        response.EnsureSuccessStatusCode();

        var result =
            await response.Content
                .ReadFromJsonAsync<List<
                    FinancialDashboardRouteBreakdownResponse>>();

        Assert.NotNull(result);

        var item =
            Assert.Single(
                result, x =>
                    x.CollectionRouteId ==
                    route.Id);

        Assert.True(
            item.IsActive);

        Assert.False(
            item.IsUnassigned);

        Assert.Equal(
            0,
            item.ActiveLoansCount);

        Assert.Equal(
            0,
            item.ActiveClientsCount);

        Assert.Equal(
            0m,
            item.ContractualBalanceOutstanding);

        Assert.Equal(
            0m,
            item.CollectionAmountDue);
    }

    [Fact]
    public async Task Routes_UnassignedActivePortfolio_AppearsInUnassignedBucket()
    {
        var context =
            await CreateContextAsync();

        var investor =
            await CreateInvestorAsync(
                context.Client,
                "Unassigned Investor");

        var client =
            await CreateClientAsync(
                context.Client);

        await CreateLoanAsync(
            context.Client,
            investor.Id,
            client.Id,
            DateTime.UtcNow.Date);

        var response =
            await context.Client.GetAsync(
                "/api/dashboard/routes");

        response.EnsureSuccessStatusCode();

        var result =
            await response.Content
                .ReadFromJsonAsync<List<
                    FinancialDashboardRouteBreakdownResponse>>();

        Assert.NotNull(result);

        var item =
            Assert.Single(
                result, x =>
                    x.IsUnassigned);

        Assert.Null(
            item.CollectionRouteId);

        Assert.Equal(
            "Sin ruta",
            item.CollectionRouteName);

        Assert.False(
            item.IsActive);

        Assert.Equal(
            1,
            item.ActiveLoansCount);

        Assert.Equal(
            1,
            item.ActiveClientsCount);

        Assert.Equal(
            1300m,
            item.ContractualBalanceOutstanding);

        Assert.Equal(
            1300m,
            item.TotalOutstanding);
    }

    [Fact]
    public async Task Routes_OverdueLoanWithLateFee_ReturnsOperationalMetrics()
    {
        var context =
            await CreateContextAsync();

        var route =
            await CreateRouteAsync(
                context.TenantId,
                "Ruta Mora");

        var investor =
            await CreateInvestorAsync(
                context.Client,
                "Late Fee Investor");

        var client =
            await CreateClientAsync(
                context.Client,
                route.Id);

        await CreateLoanAsync(
            context.Client,
            investor.Id,
            client.Id,
            DateTime.UtcNow.Date.AddDays(-8),
            lateFeeEnabled: true,
            lateFeeAmount: 20m);

        /*
         * GetRoutesAsync termina llamando
         * GetActivePortfolioAsync, que materializa
         * la mora pendiente antes de construir
         * las métricas.
         */
        var response =
            await context.Client.GetAsync(
                "/api/dashboard/routes");

        response.EnsureSuccessStatusCode();

        var result =
            await response.Content
                .ReadFromJsonAsync<List<
                    FinancialDashboardRouteBreakdownResponse>>();

        Assert.NotNull(result);

        var item =
            Assert.Single(
                result, x =>
                    x.CollectionRouteId ==
                    route.Id);

        Assert.Equal(
            1,
            item.OverdueLoansCount);

        Assert.Equal(
            1300m,
            item.ContractualBalanceOutstanding);

        Assert.Equal(
            20m,
            item.LateFeeBalanceOutstanding);

        Assert.Equal(
            1320m,
            item.TotalOutstanding);

        Assert.Equal(
            100m,
            item.NextInstallmentAmountDue);

        Assert.Equal(
            120m,
            item.CollectionAmountDue);

        Assert.Equal(
            100m,
            item.OverdueContractualAmount);

        Assert.Equal(
            120m,
            item.TotalOverdueAmountDue);

        Assert.Equal(
            7.69m,
            item.DelinquencyRate);
    }

    [Fact]
    public async Task Routes_InactiveEmptyRoute_IsHidden()
    {
        var context =
            await CreateContextAsync();

        var route =
            await CreateRouteAsync(
                context.TenantId,
                "Ruta Inactiva Vacía");

        await SetRouteActiveAsync(
            context.TenantId,
            route.Id,
            false);

        var response =
            await context.Client.GetAsync(
                "/api/dashboard/routes");

        response.EnsureSuccessStatusCode();

        var result =
            await response.Content
                .ReadFromJsonAsync<List<
                    FinancialDashboardRouteBreakdownResponse>>();

        Assert.NotNull(result);

        Assert.DoesNotContain(
            result,
            x =>
                x.CollectionRouteId ==
                route.Id);
    }

    [Fact]
    public async Task Routes_InactiveRouteWithActivePortfolio_RemainsVisible()
    {
        var context =
            await CreateContextAsync();

        var route =
            await CreateRouteAsync(
                context.TenantId,
                "Ruta Inactiva Con Cartera");

        var investor =
            await CreateInvestorAsync(
                context.Client,
                "Inactive Route Investor");

        var client =
            await CreateClientAsync(
                context.Client,
                route.Id);

        await CreateLoanAsync(
            context.Client,
            investor.Id,
            client.Id,
            DateTime.UtcNow.Date);

        /*
         * Primero se asigna el cliente cuando la ruta
         * está activa. Luego simulamos que la ruta fue
         * desactivada pero todavía conserva cartera.
         */
        await SetRouteActiveAsync(
            context.TenantId,
            route.Id,
            false);

        var response =
            await context.Client.GetAsync(
                "/api/dashboard/routes");

        response.EnsureSuccessStatusCode();

        var result =
            await response.Content
                .ReadFromJsonAsync<List<
                    FinancialDashboardRouteBreakdownResponse>>();

        Assert.NotNull(result);

        var item =
            Assert.Single(
                result, x =>
                    x.CollectionRouteId ==
                    route.Id);

        Assert.False(
            item.IsActive);

        Assert.False(
            item.IsUnassigned);

        Assert.Equal(
            1,
            item.ActiveLoansCount);

        Assert.Equal(
            1300m,
            item.ContractualBalanceOutstanding);
    }

    // ============================================================
    // AUTHORIZATION
    // ============================================================

    [Fact]
    public async Task Breakdowns_WithoutToken_ReturnUnauthorized()
    {
        var client =
            _factory.CreateClient();

        var investorsResponse =
            await client.GetAsync(
                "/api/dashboard/investors");

        var routesResponse =
            await client.GetAsync(
                "/api/dashboard/routes");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            investorsResponse.StatusCode);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            routesResponse.StatusCode);
    }

    [Fact]
    public async Task Breakdowns_Collector_ReturnForbidden()
    {
        var context =
            await CreateContextAsync();

        var collector =
            await CreateCollectorAsync(
                context);

        var collectorClient =
            await LoginCollectorAsync(
                context.TenantId,
                collector);

        var investorsResponse =
            await collectorClient.GetAsync(
                "/api/dashboard/investors");

        var routesResponse =
            await collectorClient.GetAsync(
                "/api/dashboard/routes");

        Assert.Equal(
            HttpStatusCode.Forbidden,
            investorsResponse.StatusCode);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            routesResponse.StatusCode);
    }

    // ============================================================
    // SETUP HELPERS
    // ============================================================

    private async Task<TestTenantContext>
        CreateContextAsync()
    {
        return await TestAuthenticationHelper
            .CreateAdministratorContextAsync(
                _factory);
    }

    private async Task<InvestorResponse>
        CreateInvestorAsync(
            HttpClient client,
            string name)
    {
        var response =
            await client.PostAsJsonAsync(
                "/api/investors",
                new CreateInvestorRequest
                {
                    Name =
                        $"{name} {Guid.NewGuid():N}",

                    Phone =
                        "8095551000",

                    Identification =
                        $"INV-{Guid.NewGuid():N}"
                });

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
            Guid? collectionRouteId = null)
    {
        var response =
            await client.PostAsJsonAsync(
                "/api/clients",
                new CreateClientRequest
                {
                    FirstName =
                        "Dashboard",

                    LastName =
                        $"Breakdown {Guid.NewGuid():N}",

                    Phone =
                        $"809{Random.Shared.Next(
                            1000000,
                            9999999)}",

                    Address =
                        "Financial dashboard breakdown test",

                    CollectionRouteId =
                        collectionRouteId
                });

        response.EnsureSuccessStatusCode();

        var created =
            await response.Content
                .ReadFromJsonAsync<
                    ClientResponse>();

        Assert.NotNull(created);

        return created;
    }

    private static async Task<LoanResponse>
        CreateLoanAsync(
            HttpClient client,
            Guid investorId,
            Guid clientId,
            DateTime startDate,
            bool lateFeeEnabled = false,
            decimal lateFeeAmount = 0m)
    {
        var response =
            await client.PostAsJsonAsync(
                "/api/loans",
                new CreateLoanRequest
                {
                    InvestorId =
                        investorId,

                    ClientId =
                        clientId,

                    PrincipalAmount =
                        1000m,

                    InstallmentAmount =
                        100m,

                    TotalInstallments =
                        13,

                    PaymentFrequency =
                        PaymentFrequency.Weekly,

                    StartDate =
                        startDate,

                    Notes =
                        "Financial dashboard breakdown test",

                    LateFeeEnabled =
                        lateFeeEnabled,

                    LateFeeCalculationType =
                        LateFeeCalculationType
                            .FixedAmountPerInstallment,

                    LateFeeAmount =
                        lateFeeAmount,

                    LateFeeGraceDays =
                        0
                });

        response.EnsureSuccessStatusCode();

        var loan =
            await response.Content
                .ReadFromJsonAsync<
                    LoanResponse>();

        Assert.NotNull(loan);

        return loan;
    }

    private static async Task<HttpResponseMessage>
        CreatePaymentAsync(
            HttpClient client,
            Guid loanId,
            decimal amount,
            DateTime paymentDate,
            PaymentType paymentType)
    {
        return await client.PostAsJsonAsync(
            "/api/payments",
            new CreatePaymentRequest
            {
                LoanId =
                    loanId,

                Amount =
                    amount,

                PaymentDate =
                    paymentDate,

                PaymentType =
                    paymentType,

                Notes =
                    "Dashboard breakdown payment"
            });
    }

    // ============================================================
    // DIRECT DB HELPERS
    // ============================================================

    private async Task<CollectionRoute>
        CreateRouteAsync(
            Guid tenantId,
            string name)
    {
        using var scope =
            _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<SanesDbContext>();

        var route =
            new CollectionRoute
            {
                TenantId =
                    tenantId,

                Name =
                    $"{name} {Guid.NewGuid():N}",

                Description =
                    "Financial dashboard breakdown test",

                IsActive =
                    true,

                CreatedAt =
                    DateTime.UtcNow,

                UpdatedAt =
                    DateTime.UtcNow
            };

        dbContext.CollectionRoutes.Add(
            route);

        await dbContext.SaveChangesAsync();

        return route;
    }

    private async Task SetRouteActiveAsync(
        Guid tenantId,
        Guid routeId,
        bool isActive)
    {
        using var scope =
            _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<SanesDbContext>();

        var route =
            await dbContext.CollectionRoutes
                .SingleAsync(x =>
                    x.TenantId == tenantId &&
                    x.Id == routeId);

        route.IsActive =
            isActive;

        route.UpdatedAt =
            DateTime.UtcNow;

        await dbContext.SaveChangesAsync();
    }

    private async Task SetInvestorActiveAsync(
        Guid tenantId,
        Guid investorId,
        bool isActive)
    {
        using var scope =
            _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<SanesDbContext>();

        var investor =
            await dbContext.Investors
                .SingleAsync(x =>
                    x.TenantId == tenantId &&
                    x.Id == investorId);

        investor.IsActive =
            isActive;

        investor.UpdatedAt =
            DateTime.UtcNow;

        await dbContext.SaveChangesAsync();
    }

    // ============================================================
    // EARLY SETTLEMENT
    // ============================================================

    private static async Task<
        EarlySettlementQuoteResponse>
        QuoteAsync(
            HttpClient client,
            Guid loanId,
            EarlySettlementDiscountType discountType,
            decimal discountValue)
    {
        var response =
            await client.PostAsJsonAsync(
                $"/api/loans/{loanId}/early-settlement/quote",
                new EarlySettlementQuoteRequest
                {
                    DiscountType =
                        discountType,

                    DiscountValue =
                        discountValue
                });

        response.EnsureSuccessStatusCode();

        var quote =
            await response.Content
                .ReadFromJsonAsync<
                    EarlySettlementQuoteResponse>();

        Assert.NotNull(quote);

        return quote;
    }

    private static async Task<
        EarlySettlementResponse>
        ExecuteAsync(
            HttpClient client,
            Guid loanId,
            EarlySettlementDiscountType discountType,
            decimal discountValue,
            decimal expectedSettlementAmount)
    {
        var response =
            await client.PostAsJsonAsync(
                $"/api/loans/{loanId}/early-settlement",
                new EarlySettlementExecuteRequest
                {
                    DiscountType =
                        discountType,

                    DiscountValue =
                        discountValue,

                    ExpectedSettlementAmount =
                        expectedSettlementAmount,

                    Reason =
                        "Financial dashboard breakdown test"
                });

        response.EnsureSuccessStatusCode();

        var settlement =
            await response.Content
                .ReadFromJsonAsync<
                    EarlySettlementResponse>();

        Assert.NotNull(settlement);

        return settlement;
    }

    // ============================================================
    // COLLECTOR AUTH
    // ============================================================

    private sealed record CollectorCredentials(
        Guid Id,
        string Username,
        string Password);

    private async Task<CollectorCredentials>
        CreateCollectorAsync(
            TestTenantContext context)
    {
        var suffix =
            Guid.NewGuid()
                .ToString("N");

        var username =
            $"breakdown-collector-{suffix}";

        var password =
            TestAuthenticationHelper
                .DefaultPassword;

        var response =
            await context.Client.PostAsJsonAsync(
                "/api/app-users",
                new
                {
                    name =
                        $"Breakdown Collector {suffix}",

                    username,

                    password,

                    email =
                        $"breakdown-{suffix}@sanes.test",

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

        var client =
            _factory.CreateClient();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                auth.AccessToken);

        return client;
    }
}