using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Sanes.Api.IntegrationTests.Helpers;
using Sanes.Application.Authentication.DTOs;
using Sanes.Application.Clients.DTOs;
using Sanes.Application.CollectionRoutes.DTOs;
using Sanes.Application.FieldCollections.DTOs;
using Sanes.Application.FinancialReports.DTOs;
using Sanes.Application.Investors.DTOs;
using Sanes.Application.Loans.DTOs;
using Sanes.Application.Payments.DTOs;
using Sanes.Domain.Enums;

namespace Sanes.Api.IntegrationTests.FinancialReports;

public class FinancialReportsTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public FinancialReportsTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ============================================================
    // PORTFOLIO
    // ============================================================

    [Fact]
    public async Task Portfolio_ReturnsActivePortfolioAndSummary()
    {
        var context = await CreateContextAsync();

        var route =
            await CreateRouteAsync(context.Client);

        var investor =
            await CreateInvestorAsync(context.Client);

        var client =
            await CreateClientAsync(
                context.Client,
                route.Id,
                "Portfolio",
                "Client");

        var loan =
            await CreateLoanAsync(
                context.Client,
                investor.Id,
                client.Id,
                DateTime.UtcNow.Date);

        var response =
            await context.Client.GetAsync(
                "/api/reports/portfolio");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    FinancialPortfolioReportResponse>();

        Assert.NotNull(result);

        var item =
            Assert.Single(
                result.Items,
                x => x.LoanId == loan.Id);

        Assert.Equal(
            investor.Id,
            item.InvestorId);

        Assert.Equal(
            route.Id,
            item.CollectionRouteId);

        Assert.Equal(
            1000m,
            item.PrincipalAmount);

        Assert.Equal(
            1300m,
            item.ContractualAmount);

        Assert.Equal(
            1300m,
            item.ContractualBalanceOutstanding);

        Assert.Equal(
            1300m,
            item.TotalOutstanding);

        Assert.Equal(
            1,
            result.Summary.LoansCount);

        Assert.Equal(
            1,
            result.Summary.ClientsCount);

        Assert.Equal(
            1000m,
            result.Summary.PrincipalAmount);

        Assert.Equal(
            1300m,
            result.Summary.ContractualAmount);

        Assert.Equal(
            1300m,
            result.Summary.ContractualBalanceOutstanding);
    }

    [Fact]
    public async Task Portfolio_InvestorFilter_ReturnsOnlySelectedInvestor()
    {
        var context = await CreateContextAsync();

        var route =
            await CreateRouteAsync(context.Client);

        var investor1 =
            await CreateInvestorAsync(context.Client);

        var investor2 =
            await CreateInvestorAsync(context.Client);

        var client1 =
            await CreateClientAsync(
                context.Client,
                route.Id,
                "Investor",
                "One");

        var client2 =
            await CreateClientAsync(
                context.Client,
                route.Id,
                "Investor",
                "Two");

        var loan1 =
            await CreateLoanAsync(
                context.Client,
                investor1.Id,
                client1.Id,
                DateTime.UtcNow.Date);

        var loan2 =
            await CreateLoanAsync(
                context.Client,
                investor2.Id,
                client2.Id,
                DateTime.UtcNow.Date);

        var response =
            await context.Client.GetAsync(
                "/api/reports/portfolio" +
                $"?investorId={investor1.Id}");

        response.EnsureSuccessStatusCode();

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    FinancialPortfolioReportResponse>();

        Assert.NotNull(result);

        var item =
            Assert.Single(result.Items);

        Assert.Equal(
            loan1.Id,
            item.LoanId);

        Assert.Equal(
            investor1.Id,
            item.InvestorId);

        Assert.DoesNotContain(
            result.Items,
            x => x.LoanId == loan2.Id);
    }

    [Fact]
    public async Task Portfolio_RouteFilter_ReturnsOnlyCurrentRoute()
    {
        var context = await CreateContextAsync();

        var route1 =
            await CreateRouteAsync(context.Client);

        var route2 =
            await CreateRouteAsync(context.Client);

        var investor =
            await CreateInvestorAsync(context.Client);

        var client1 =
            await CreateClientAsync(
                context.Client,
                route1.Id,
                "Route",
                "One");

        var client2 =
            await CreateClientAsync(
                context.Client,
                route2.Id,
                "Route",
                "Two");

        var loan1 =
            await CreateLoanAsync(
                context.Client,
                investor.Id,
                client1.Id,
                DateTime.UtcNow.Date);

        var loan2 =
            await CreateLoanAsync(
                context.Client,
                investor.Id,
                client2.Id,
                DateTime.UtcNow.Date);

        var response =
            await context.Client.GetAsync(
                "/api/reports/portfolio" +
                $"?collectionRouteId={route1.Id}");

        response.EnsureSuccessStatusCode();

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    FinancialPortfolioReportResponse>();

        Assert.NotNull(result);

        var item =
            Assert.Single(result.Items);

        Assert.Equal(
            loan1.Id,
            item.LoanId);

        Assert.Equal(
            route1.Id,
            item.CollectionRouteId);

        Assert.DoesNotContain(
            result.Items,
            x => x.LoanId == loan2.Id);
    }

    [Fact]
    public async Task Portfolio_OverdueOnly_ExcludesCurrentLoans()
    {
        var context = await CreateContextAsync();

        var route =
            await CreateRouteAsync(context.Client);

        var investor =
            await CreateInvestorAsync(context.Client);

        var overdueClient =
            await CreateClientAsync(
                context.Client,
                route.Id,
                "Overdue",
                "Client");

        var currentClient =
            await CreateClientAsync(
                context.Client,
                route.Id,
                "Current",
                "Client");

        var overdueLoan =
            await CreateLoanAsync(
                context.Client,
                investor.Id,
                overdueClient.Id,
                DateTime.UtcNow.Date.AddDays(-8));

        var currentLoan =
            await CreateLoanAsync(
                context.Client,
                investor.Id,
                currentClient.Id,
                DateTime.UtcNow.Date);

        var response =
            await context.Client.GetAsync(
                "/api/reports/portfolio" +
                "?overdueOnly=true");

        response.EnsureSuccessStatusCode();

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    FinancialPortfolioReportResponse>();

        Assert.NotNull(result);

        Assert.Contains(
            result.Items,
            x => x.LoanId == overdueLoan.Id);

        Assert.DoesNotContain(
            result.Items,
            x => x.LoanId == currentLoan.Id);
    }

    // ============================================================
    // DELINQUENCY
    // ============================================================

    [Fact]
    public async Task Delinquency_ReturnsAgingAndUsesFullPortfolioAsRateBase()
    {
        var context = await CreateContextAsync();

        var route =
            await CreateRouteAsync(context.Client);

        var investor =
            await CreateInvestorAsync(context.Client);

        var overdueClient =
            await CreateClientAsync(
                context.Client,
                route.Id,
                "Delinquent",
                "Client");

        var healthyClient =
            await CreateClientAsync(
                context.Client,
                route.Id,
                "Healthy",
                "Client");

        var overdueLoan =
            await CreateLoanAsync(
                context.Client,
                investor.Id,
                overdueClient.Id,
                DateTime.UtcNow.Date.AddDays(-8));

        await CreateLoanAsync(
            context.Client,
            investor.Id,
            healthyClient.Id,
            DateTime.UtcNow.Date);

        var response =
            await context.Client.GetAsync(
                "/api/reports/delinquency");

        response.EnsureSuccessStatusCode();

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    FinancialDelinquencyReportResponse>();

        Assert.NotNull(result);

        Assert.Equal(
            2,
            result.Summary.LoansCount);

        Assert.Equal(
            2,
            result.Summary.ClientsCount);

        Assert.Equal(
            2600m,
            result.Summary.ContractualBalanceOutstanding);

        Assert.Equal(
            100m,
            result.Summary.OverdueContractualAmount);

        Assert.Equal(
            3.85m,
            result.Summary.DelinquencyRate);

        var item =
            Assert.Single(result.Items);

        Assert.Equal(
            overdueLoan.Id,
            item.LoanId);

        Assert.Equal(
            1,
            item.DaysOverdue);

        Assert.Equal(
            "1-7",
            item.AgingBucket);

        var aging =
            Assert.Single(result.Aging);

        Assert.Equal(
            "1-7",
            aging.AgingBucket);

        Assert.Equal(
            1,
            aging.LoansCount);

        Assert.Equal(
            100m,
            aging.OverdueContractualAmount);
    }

    // ============================================================
    // COLLECTIONS
    // ============================================================

    [Fact]
    public async Task Collections_ReturnsOnlyPaymentsInsideRequestedPeriod()
    {
        var context = await CreateContextAsync();

        var route =
            await CreateRouteAsync(context.Client);

        var investor =
            await CreateInvestorAsync(context.Client);

        var client =
            await CreateClientAsync(
                context.Client,
                route.Id,
                "Collections",
                "Period");

        var loan =
            await CreateLoanAsync(
                context.Client,
                investor.Id,
                client.Id,
                DateTime.UtcNow.Date.AddDays(-20));

        var today =
            DateTime.UtcNow.Date;

        await CreateAdministrativePaymentAsync(
            context.Client,
            loan.Id,
            100m,
            today.AddDays(-1));

        await CreateAdministrativePaymentAsync(
            context.Client,
            loan.Id,
            100m,
            today);

        var date =
            DateOnly.FromDateTime(today);

        var response =
            await context.Client.GetAsync(
                "/api/reports/collections" +
                $"?from={date:yyyy-MM-dd}" +
                $"&to={date:yyyy-MM-dd}");

        response.EnsureSuccessStatusCode();

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    FinancialCollectionsReportResponse>();

        Assert.NotNull(result);

        Assert.Equal(
            date,
            result.From);

        Assert.Equal(
            date,
            result.To);

        Assert.Equal(
            1,
            result.Summary.PaymentsCount);

        Assert.Equal(
            100m,
            result.Summary.CashCollected);

        var item =
            Assert.Single(result.Items);

        Assert.Equal(
            loan.Id,
            item.LoanId);

        Assert.Equal(
            100m,
            item.CashCollected);
    }

    [Fact]
    public async Task Collections_InvestorFilter_ReturnsOnlySelectedInvestor()
    {
        var context = await CreateContextAsync();

        var route =
            await CreateRouteAsync(context.Client);

        var investor1 =
            await CreateInvestorAsync(context.Client);

        var investor2 =
            await CreateInvestorAsync(context.Client);

        var client1 =
            await CreateClientAsync(
                context.Client,
                route.Id,
                "Collection",
                "One");

        var client2 =
            await CreateClientAsync(
                context.Client,
                route.Id,
                "Collection",
                "Two");

        var loan1 =
            await CreateLoanAsync(
                context.Client,
                investor1.Id,
                client1.Id,
                DateTime.UtcNow.Date);

        var loan2 =
            await CreateLoanAsync(
                context.Client,
                investor2.Id,
                client2.Id,
                DateTime.UtcNow.Date);

        await CreateAdministrativePaymentAsync(
            context.Client,
            loan1.Id,
            100m,
            DateTime.UtcNow);

        await CreateAdministrativePaymentAsync(
            context.Client,
            loan2.Id,
            100m,
            DateTime.UtcNow);

        var today =
            DateOnly.FromDateTime(
                DateTime.UtcNow);

        var response =
            await context.Client.GetAsync(
                "/api/reports/collections" +
                $"?from={today:yyyy-MM-dd}" +
                $"&to={today:yyyy-MM-dd}" +
                $"&investorId={investor1.Id}");

        response.EnsureSuccessStatusCode();

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    FinancialCollectionsReportResponse>();

        Assert.NotNull(result);

        var item =
            Assert.Single(result.Items);

        Assert.Equal(
            loan1.Id,
            item.LoanId);

        Assert.Equal(
            investor1.Id,
            item.InvestorId);

        Assert.DoesNotContain(
            result.Items,
            x => x.LoanId == loan2.Id);
    }

    [Fact]
    public async Task Collections_FieldPayment_PreservesCollectorAndRoute()
    {
        var context = await CreateContextAsync();

        var route =
            await CreateRouteAsync(context.Client);

        var collector =
            await CreateCollectorAsync(context);

        await AssignRouteAsync(
            context.Client,
            collector.Id,
            route.Id);

        var investor =
            await CreateInvestorAsync(context.Client);

        var client =
            await CreateClientAsync(
                context.Client,
                route.Id,
                "Field",
                "Collection");

        var loan =
            await CreateLoanAsync(
                context.Client,
                investor.Id,
                client.Id,
                DateTime.UtcNow.Date);

        var collectorClient =
            await LoginCollectorAsync(
                context.TenantId,
                collector);

        var paymentResponse =
            await collectorClient.PostAsJsonAsync(
                "/api/field-collections/payments",
                new CreateFieldCollectionPaymentRequest
                {
                    CollectionRouteId =
                        route.Id,

                    LoanId =
                        loan.Id,

                    Amount =
                        100m,

                    PaymentType =
                        PaymentType.Regular
                });

        paymentResponse.EnsureSuccessStatusCode();

        var today =
            DateOnly.FromDateTime(
                DateTime.UtcNow);

        var response =
            await context.Client.GetAsync(
                "/api/reports/collections" +
                $"?from={today:yyyy-MM-dd}" +
                $"&to={today:yyyy-MM-dd}" +
                $"&collectorId={collector.Id}" +
                $"&collectionRouteId={route.Id}");

        response.EnsureSuccessStatusCode();

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    FinancialCollectionsReportResponse>();

        Assert.NotNull(result);

        var item =
            Assert.Single(result.Items);

        Assert.Equal(
            loan.Id,
            item.LoanId);

        Assert.Equal(
            collector.Id,
            item.CollectedByAppUserId);

        Assert.Equal(
            route.Id,
            item.CollectionRouteId);

        Assert.False(
            string.IsNullOrWhiteSpace(
                item.CollectorName));

        Assert.False(
            string.IsNullOrWhiteSpace(
                item.CollectionRouteName));

        Assert.False(
            string.IsNullOrWhiteSpace(
                item.ReceiptNumber));

        Assert.Equal(
            100m,
            item.CashCollected);
    }

    [Fact]
    public async Task Collections_AreTenantIsolated()
    {
        var tenant1 =
            await CreateContextAsync();

        var tenant2 =
            await CreateContextAsync();

        var setup1 =
            await CreateLoanScenarioAsync(
                tenant1,
                "Tenant",
                "One");

        var setup2 =
            await CreateLoanScenarioAsync(
                tenant2,
                "Tenant",
                "Two");

        await CreateAdministrativePaymentAsync(
            tenant1.Client,
            setup1.Loan.Id,
            100m,
            DateTime.UtcNow);

        await CreateAdministrativePaymentAsync(
            tenant2.Client,
            setup2.Loan.Id,
            100m,
            DateTime.UtcNow);

        var today =
            DateOnly.FromDateTime(
                DateTime.UtcNow);

        var response =
            await tenant1.Client.GetAsync(
                "/api/reports/collections" +
                $"?from={today:yyyy-MM-dd}" +
                $"&to={today:yyyy-MM-dd}");

        response.EnsureSuccessStatusCode();

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    FinancialCollectionsReportResponse>();

        Assert.NotNull(result);

        Assert.Contains(
            result.Items,
            x => x.LoanId == setup1.Loan.Id);

        Assert.DoesNotContain(
            result.Items,
            x => x.LoanId == setup2.Loan.Id);
    }

    // ============================================================
    // INVESTOR STATEMENT
    // ============================================================

    [Fact]
    public async Task InvestorStatement_ReturnsHistoricalAndCurrentMetrics()
    {
        var context = await CreateContextAsync();

        var route =
            await CreateRouteAsync(context.Client);

        var investor =
            await CreateInvestorAsync(context.Client);

        var client =
            await CreateClientAsync(
                context.Client,
                route.Id,
                "Investor",
                "Statement");

        var loan =
            await CreateLoanAsync(
                context.Client,
                investor.Id,
                client.Id,
                DateTime.UtcNow.Date);

        await CreateAdministrativePaymentAsync(
            context.Client,
            loan.Id,
            100m,
            DateTime.UtcNow);

        var response =
            await context.Client.GetAsync(
                $"/api/reports/investors/{investor.Id}");

        response.EnsureSuccessStatusCode();

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    FinancialInvestorStatementResponse>();

        Assert.NotNull(result);

        Assert.Equal(
            investor.Id,
            result.InvestorId);

        Assert.Equal(
            1000m,
            result.PrincipalOriginated);

        Assert.Equal(
            1300m,
            result.ContractualAmountOriginated);

        Assert.Equal(
            300m,
            result.GrossContractualInterest);

        Assert.Equal(
            0m,
            result.EarlySettlementDiscounts);

        Assert.Equal(
            300m,
            result.NetContractualInterest);

        Assert.Equal(
            100m,
            result.CashCollected);

        Assert.Equal(
            100m,
            result.ContractualCashCollected);

        Assert.Equal(
            0m,
            result.LateFeesCollected);

        Assert.Equal(
            1,
            result.ActiveLoansCount);

        Assert.Equal(
            1,
            result.ActiveClientsCount);

        Assert.Equal(
            1200m,
            result.ContractualBalanceOutstanding);

        Assert.Equal(
            1200m,
            result.TotalOutstanding);

        var activeLoan =
            Assert.Single(result.ActiveLoans);

        Assert.Equal(
            loan.Id,
            activeLoan.LoanId);
    }

    [Fact]
    public async Task InvestorStatement_InvestorFromAnotherTenant_ReturnsNotFound()
    {
        var tenant1 =
            await CreateContextAsync();

        var tenant2 =
            await CreateContextAsync();

        var investorTenant2 =
            await CreateInvestorAsync(
                tenant2.Client);

        var response =
            await tenant1.Client.GetAsync(
                $"/api/reports/investors/" +
                $"{investorTenant2.Id}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    // ============================================================
    // VALIDATION
    // ============================================================

    [Fact]
    public async Task Collections_FromGreaterThanTo_ReturnsBadRequest()
    {
        var context =
            await CreateContextAsync();

        var response =
            await context.Client.GetAsync(
                "/api/reports/collections" +
                "?from=2026-09-18" +
                "&to=2026-09-17");

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task Collections_PeriodGreaterThan366Days_ReturnsBadRequest()
    {
        var context =
            await CreateContextAsync();

        var response =
            await context.Client.GetAsync(
                "/api/reports/collections" +
                "?from=2025-01-01" +
                "&to=2026-01-02");

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    // ============================================================
    // AUTHORIZATION
    // ============================================================

    [Theory]
    [InlineData("/api/reports/portfolio")]
    [InlineData("/api/reports/delinquency")]
    [InlineData(
        "/api/reports/collections?from=2026-09-01&to=2026-09-30")]
    [InlineData(
        "/api/reports/investors/11111111-1111-1111-1111-111111111111")]
    public async Task Reports_WithoutToken_ReturnUnauthorized(
        string url)
    {
        var client =
            _factory.CreateClient();

        var response =
            await client.GetAsync(url);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Theory]
    [InlineData("/api/reports/portfolio")]
    [InlineData("/api/reports/delinquency")]
    [InlineData(
        "/api/reports/collections?from=2026-09-01&to=2026-09-30")]
    [InlineData(
        "/api/reports/investors/11111111-1111-1111-1111-111111111111")]
    public async Task Reports_Collector_ReturnsForbidden(
        string url)
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

        var response =
            await collectorClient.GetAsync(url);

        Assert.Equal(
            HttpStatusCode.Forbidden,
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

    private static async Task<InvestorResponse>
        CreateInvestorAsync(
            HttpClient client)
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
                        $"Report Investor {suffix}",
                    Phone =
                        "8095551000",
                    Identification =
                        $"RPT-{suffix}"
                });

        response.EnsureSuccessStatusCode();

        var investor =
            await response.Content
                .ReadFromJsonAsync<
                    InvestorResponse>();

        Assert.NotNull(investor);

        return investor;
    }

    private static async Task<CollectionRouteResponse>
        CreateRouteAsync(
            HttpClient client)
    {
        var response =
            await client.PostAsJsonAsync(
                "/api/collection-routes",
                new CreateCollectionRouteRequest
                {
                    Name =
                        $"Report Route {Guid.NewGuid():N}",
                    Description =
                        "Financial reports integration test"
                });

        response.EnsureSuccessStatusCode();

        var route =
            await response.Content
                .ReadFromJsonAsync<
                    CollectionRouteResponse>();

        Assert.NotNull(route);

        return route;
    }

    private static async Task<ClientResponse>
        CreateClientAsync(
            HttpClient client,
            Guid routeId,
            string firstName,
            string lastName)
    {
        var response =
            await client.PostAsJsonAsync(
                "/api/clients",
                new CreateClientRequest
                {
                    FirstName =
                        firstName,

                    LastName =
                        $"{lastName} {Guid.NewGuid():N}",

                    Phone =
                        $"809{Random.Shared.Next(
                            1000000,
                            9999999)}",

                    Address =
                        "Financial Reports Test",

                    CollectionRouteId =
                        routeId
                });

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
            Guid clientId,
            DateTime startDate)
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
                        "Financial reports integration test"
                });

        response.EnsureSuccessStatusCode();

        var loan =
            await response.Content
                .ReadFromJsonAsync<
                    LoanResponse>();

        Assert.NotNull(loan);

        return loan;
    }

    private static async Task
        CreateAdministrativePaymentAsync(
            HttpClient client,
            Guid loanId,
            decimal amount,
            DateTime paymentDate)
    {
        var response =
            await client.PostAsJsonAsync(
                "/api/payments",
                new CreatePaymentRequest
                {
                    LoanId =
                        loanId,

                    Amount =
                        amount,

                    PaymentDate =
                        DateTime.SpecifyKind(
                            paymentDate,
                            DateTimeKind.Utc),

                    PaymentType =
                        PaymentType.Regular,

                    Notes =
                        "Financial reports integration test"
                });

        response.EnsureSuccessStatusCode();
    }

    private async Task<CollectorCredentials>
        CreateCollectorAsync(
            TestTenantContext context)
    {
        var suffix =
            Guid.NewGuid()
                .ToString("N");

        var username =
            $"report-collector-{suffix}";

        var password =
            TestAuthenticationHelper
                .DefaultPassword;

        var response =
            await context.Client.PostAsJsonAsync(
                "/api/app-users",
                new
                {
                    name =
                        $"Report Collector {suffix}",
                    username,
                    password,
                    email =
                        $"report-{suffix}@sanes.test",
                    phone =
                        "8095551234",
                    role =
                        (int)AppUserRole.Collector
                });

        response.EnsureSuccessStatusCode();

        var json =
            await response.Content
                .ReadFromJsonAsync<JsonElement>();

        return new CollectorCredentials(
            json.GetProperty("id")
                .GetGuid(),
            username,
            password);
    }

    private static async Task
        AssignRouteAsync(
            HttpClient administratorClient,
            Guid collectorId,
            Guid routeId)
    {
        var response =
            await administratorClient.PostAsync(
                $"/api/app-users/{collectorId}" +
                $"/collection-routes/{routeId}",
                null);

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);
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
                .ReadFromJsonAsync<AuthResponse>();

        Assert.NotNull(auth);

        var client =
            _factory.CreateClient();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                auth.AccessToken);

        return client;
    }

    private static async Task<LoanScenario>
        CreateLoanScenarioAsync(
            TestTenantContext context,
            string firstName,
            string lastName)
    {
        var route =
            await CreateRouteAsync(
                context.Client);

        var investor =
            await CreateInvestorAsync(
                context.Client);

        var client =
            await CreateClientAsync(
                context.Client,
                route.Id,
                firstName,
                lastName);

        var loan =
            await CreateLoanAsync(
                context.Client,
                investor.Id,
                client.Id,
                DateTime.UtcNow.Date);

        return new LoanScenario(
            route,
            investor,
            client,
            loan);
    }

    private sealed record CollectorCredentials(
        Guid Id,
        string Username,
        string Password);

    private sealed record LoanScenario(
        CollectionRouteResponse Route,
        InvestorResponse Investor,
        ClientResponse Client,
        LoanResponse Loan);
}