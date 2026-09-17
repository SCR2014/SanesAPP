using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Sanes.Api.IntegrationTests.Helpers;
using Sanes.Application.Authentication.DTOs;
using Sanes.Application.Clients.DTOs;
using Sanes.Application.Dashboard.DTOs;
using Sanes.Application.Investors.DTOs;
using Sanes.Application.Loans.DTOs;
using Sanes.Application.Payments.DTOs;
using Sanes.Domain.Enums;

namespace Sanes.Api.IntegrationTests.Dashboard;

public class FinancialDashboardTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public FinancialDashboardTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ============================================================
    // SUMMARY
    // ============================================================

    [Fact]
    public async Task Summary_EmptyTenant_ReturnsZeros()
    {
        var context =
            await CreateContextAsync();

        var response =
            await context.Client.GetAsync(
                "/api/dashboard/summary");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var summary =
            await response.Content
                .ReadFromJsonAsync<
                    FinancialDashboardSummaryResponse>();

        Assert.NotNull(summary);

        Assert.Equal(0, summary.ActiveLoansCount);
        Assert.Equal(0, summary.ActiveClientsCount);

        Assert.Equal(0m, summary.PrincipalOriginated);
        Assert.Equal(0m, summary.ContractualAmountOriginated);
        Assert.Equal(0m, summary.GrossContractualInterest);

        Assert.Equal(0m, summary.EarlySettlementDiscounts);
        Assert.Equal(0m, summary.NetContractualInterest);

        Assert.Equal(
            0m,
            summary.ContractualBalanceOutstanding);

        Assert.Equal(
            0m,
            summary.LateFeeBalanceOutstanding);

        Assert.Equal(
            0m,
            summary.TotalOutstanding);

        Assert.Equal(0m, summary.CashCollected);
        Assert.Equal(0m, summary.ContractualCashCollected);
        Assert.Equal(0m, summary.LateFeesCollected);

        Assert.Equal(0, summary.OverdueLoansCount);

        Assert.Equal(
            0m,
            summary.OverdueContractualAmount);

        Assert.Equal(
            0m,
            summary.DelinquencyRate);
    }

    [Fact]
    public async Task Summary_WithActiveLoan_ReturnsExpectedValues()
    {
        var scenario =
            await CreateLoanScenarioAsync(
                DateTime.UtcNow.Date);

        var response =
            await scenario.Context.Client.GetAsync(
                "/api/dashboard/summary");

        response.EnsureSuccessStatusCode();

        var summary =
            await response.Content
                .ReadFromJsonAsync<
                    FinancialDashboardSummaryResponse>();

        Assert.NotNull(summary);

        Assert.Equal(
            1,
            summary.ActiveLoansCount);

        Assert.Equal(
            1,
            summary.ActiveClientsCount);

        /*
         * Loan:
         *
         * Principal = 1000
         * 13 x 100  = 1300 contractual
         * Interest  = 300
         */
        Assert.Equal(
            1000m,
            summary.PrincipalOriginated);

        Assert.Equal(
            1300m,
            summary.ContractualAmountOriginated);

        Assert.Equal(
            300m,
            summary.GrossContractualInterest);

        Assert.Equal(
            0m,
            summary.EarlySettlementDiscounts);

        Assert.Equal(
            300m,
            summary.NetContractualInterest);

        Assert.Equal(
            1300m,
            summary.ContractualBalanceOutstanding);

        Assert.Equal(
            0m,
            summary.LateFeeBalanceOutstanding);

        Assert.Equal(
            1300m,
            summary.TotalOutstanding);

        Assert.Equal(
            0m,
            summary.CashCollected);

        Assert.Equal(
            0m,
            summary.ContractualCashCollected);

        Assert.Equal(
            0m,
            summary.LateFeesCollected);

        Assert.Equal(
            0,
            summary.OverdueLoansCount);

        Assert.Equal(
            0m,
            summary.OverdueContractualAmount);

        Assert.Equal(
            0m,
            summary.DelinquencyRate);
    }

    [Fact]
    public async Task Summary_AfterPayment_UpdatesCashAndOutstanding()
    {
        var scenario =
            await CreateLoanScenarioAsync(
                DateTime.UtcNow.Date);

        var paymentResponse =
            await CreatePaymentAsync(
                scenario.Context.Client,
                scenario.Loan.Id,
                100m,
                DateTime.UtcNow,
                PaymentType.Regular);

        Assert.Equal(
            HttpStatusCode.Created,
            paymentResponse.StatusCode);

        var response =
            await scenario.Context.Client.GetAsync(
                "/api/dashboard/summary");

        response.EnsureSuccessStatusCode();

        var summary =
            await response.Content
                .ReadFromJsonAsync<
                    FinancialDashboardSummaryResponse>();

        Assert.NotNull(summary);

        Assert.Equal(
            1,
            summary.ActiveLoansCount);

        Assert.Equal(
            1000m,
            summary.PrincipalOriginated);

        Assert.Equal(
            1300m,
            summary.ContractualAmountOriginated);

        Assert.Equal(
            300m,
            summary.GrossContractualInterest);

        Assert.Equal(
            1200m,
            summary.ContractualBalanceOutstanding);

        Assert.Equal(
            0m,
            summary.LateFeeBalanceOutstanding);

        Assert.Equal(
            1200m,
            summary.TotalOutstanding);

        Assert.Equal(
            100m,
            summary.CashCollected);

        Assert.Equal(
            100m,
            summary.ContractualCashCollected);

        Assert.Equal(
            0m,
            summary.LateFeesCollected);
    }

    [Fact]
    public async Task Summary_WithOverdueLoanAndLateFee_ReturnsDelinquencyMetrics()
    {
        /*
         * Start = today - 8
         * first due date = today - 1
         *
         * Fixed late fee = 20
         */
        var scenario =
            await CreateLoanScenarioAsync(
                DateTime.UtcNow.Date.AddDays(-8),
                lateFeeEnabled: true,
                lateFeeAmount: 20m);

        var response =
            await scenario.Context.Client.GetAsync(
                "/api/dashboard/summary");

        response.EnsureSuccessStatusCode();

        var summary =
            await response.Content
                .ReadFromJsonAsync<
                    FinancialDashboardSummaryResponse>();

        Assert.NotNull(summary);

        Assert.Equal(
            1,
            summary.ActiveLoansCount);

        Assert.Equal(
            1300m,
            summary.ContractualBalanceOutstanding);

        Assert.Equal(
            20m,
            summary.LateFeeBalanceOutstanding);

        Assert.Equal(
            1320m,
            summary.TotalOutstanding);

        Assert.Equal(
            1,
            summary.OverdueLoansCount);

        /*
         * Una cuota vencida.
         */
        Assert.Equal(
            100m,
            summary.OverdueContractualAmount);

        /*
         * 100 / 1300 * 100 = 7.69 %
         */
        Assert.Equal(
            7.69m,
            summary.DelinquencyRate);
    }

    [Fact]
    public async Task Summary_AfterEarlySettlement_AccountsForDiscountAndCash()
    {
        var scenario =
            await CreateLoanScenarioAsync(
                DateTime.UtcNow.Date);

        /*
         * Se pagan primero seis cuotas:
         *
         * 6 x 100 = 600
         */
        for (var i = 0; i < 6; i++)
        {
            var paymentResponse =
                await CreatePaymentAsync(
                    scenario.Context.Client,
                    scenario.Loan.Id,
                    100m,
                    DateTime.UtcNow,
                    PaymentType.Regular);

            Assert.Equal(
                HttpStatusCode.Created,
                paymentResponse.StatusCode);
        }

        /*
         * Saldo contractual = 700
         *
         * Perdón de 2 cuotas:
         * descuento = 200
         *
         * efectivo de settlement = 500
         */
        var quote =
            await QuoteAsync(
                scenario.Context.Client,
                scenario.Loan.Id,
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
            scenario.Context.Client,
            scenario.Loan.Id,
            EarlySettlementDiscountType
                .InstallmentWaiver,
            2m,
            quote.SettlementAmount);

        var response =
            await scenario.Context.Client.GetAsync(
                "/api/dashboard/summary");

        response.EnsureSuccessStatusCode();

        var summary =
            await response.Content
                .ReadFromJsonAsync<
                    FinancialDashboardSummaryResponse>();

        Assert.NotNull(summary);

        /*
         * El préstamo ya está Paid, por lo que
         * desaparece de la cartera activa.
         */
        Assert.Equal(
            0,
            summary.ActiveLoansCount);

        Assert.Equal(
            0,
            summary.ActiveClientsCount);

        Assert.Equal(
            1000m,
            summary.PrincipalOriginated);

        Assert.Equal(
            1300m,
            summary.ContractualAmountOriginated);

        Assert.Equal(
            300m,
            summary.GrossContractualInterest);

        Assert.Equal(
            200m,
            summary.EarlySettlementDiscounts);

        Assert.Equal(
            100m,
            summary.NetContractualInterest);

        /*
         * Cash:
         *
         * 600 pagos normales
         * 500 settlement
         * ----------------
         * 1100
         */
        Assert.Equal(
            1100m,
            summary.CashCollected);

        Assert.Equal(
            1100m,
            summary.ContractualCashCollected);

        Assert.Equal(
            0m,
            summary.LateFeesCollected);

        Assert.Equal(
            0m,
            summary.ContractualBalanceOutstanding);

        Assert.Equal(
            0m,
            summary.TotalOutstanding);
    }

    [Fact]
    public async Task Summary_IsTenantIsolated()
    {
        var tenant1 =
            await CreateContextAsync();

        var tenant2 =
            await CreateContextAsync();

        var investorId =
            await CreateInvestorAsync(
                tenant2.Client);

        var clientId =
            await CreateClientAsync(
                tenant2.Client);

        await CreateLoanAsync(
            tenant2.Client,
            investorId,
            clientId,
            DateTime.UtcNow.Date);

        /*
         * Tenant 1 no debe ver nada del Tenant 2.
         */
        var response =
            await tenant1.Client.GetAsync(
                "/api/dashboard/summary");

        response.EnsureSuccessStatusCode();

        var summary =
            await response.Content
                .ReadFromJsonAsync<
                    FinancialDashboardSummaryResponse>();

        Assert.NotNull(summary);

        Assert.Equal(
            0,
            summary.ActiveLoansCount);

        Assert.Equal(
            0m,
            summary.PrincipalOriginated);

        Assert.Equal(
            0m,
            summary.ContractualBalanceOutstanding);

        Assert.Equal(
            0m,
            summary.CashCollected);
    }

    // ============================================================
    // CASH FLOW
    // ============================================================

    [Fact]
    public async Task CashFlow_ReturnsContinuousSeriesIncludingZeroDays()
    {
        var scenario =
            await CreateLoanScenarioAsync(
                DateTime.UtcNow.Date);

        var today =
            DateOnly.FromDateTime(
                DateTime.UtcNow);

        var paymentResponse =
            await CreatePaymentAsync(
                scenario.Context.Client,
                scenario.Loan.Id,
                100m,
                DateTime.UtcNow,
                PaymentType.Regular);

        Assert.Equal(
            HttpStatusCode.Created,
            paymentResponse.StatusCode);

        var from =
            today.AddDays(-1);

        var to =
            today.AddDays(1);

        var response =
            await scenario.Context.Client.GetAsync(
                "/api/dashboard/cash-flow" +
                $"?from={from:yyyy-MM-dd}" +
                $"&to={to:yyyy-MM-dd}");

        response.EnsureSuccessStatusCode();

        var cashFlow =
            await response.Content
                .ReadFromJsonAsync<
                    FinancialDashboardCashFlowResponse>();

        Assert.NotNull(cashFlow);

        Assert.Equal(
            from,
            cashFlow.From);

        Assert.Equal(
            to,
            cashFlow.To);

        Assert.Equal(
            3,
            cashFlow.Items.Count);

        var previousDay =
            cashFlow.Items.Single(
                x =>
                    x.Date ==
                    today.AddDays(-1));

        var currentDay =
            cashFlow.Items.Single(
                x =>
                    x.Date == today);

        var nextDay =
            cashFlow.Items.Single(
                x =>
                    x.Date ==
                    today.AddDays(1));

        Assert.Equal(
            0m,
            previousDay.PrincipalOriginated);

        Assert.Equal(
            0m,
            previousDay.CashCollected);

        Assert.Equal(
            1000m,
            currentDay.PrincipalOriginated);

        Assert.Equal(
            100m,
            currentDay.CashCollected);

        Assert.Equal(
            100m,
            currentDay.ContractualCashCollected);

        Assert.Equal(
            0m,
            currentDay.LateFeesCollected);

        Assert.Equal(
            0m,
            nextDay.PrincipalOriginated);

        Assert.Equal(
            0m,
            nextDay.CashCollected);

        Assert.Equal(
            1000m,
            cashFlow.PrincipalOriginated);

        Assert.Equal(
            100m,
            cashFlow.CashCollected);

        Assert.Equal(
            100m,
            cashFlow.ContractualCashCollected);

        Assert.Equal(
            0m,
            cashFlow.LateFeesCollected);
    }

    [Fact]
    public async Task CashFlow_UsesPaymentDateForCashCollected()
    {
        var today =
            DateTime.UtcNow.Date;

        var scenario =
            await CreateLoanScenarioAsync(
                today.AddDays(-8));

        var paymentDate =
            today.AddDays(-1);

        var paymentResponse =
            await CreatePaymentAsync(
                scenario.Context.Client,
                scenario.Loan.Id,
                100m,
                paymentDate,
                PaymentType.Regular);

        Assert.Equal(
            HttpStatusCode.Created,
            paymentResponse.StatusCode);

        var from =
            DateOnly.FromDateTime(
                paymentDate);

        var to =
            DateOnly.FromDateTime(
                today);

        var response =
            await scenario.Context.Client.GetAsync(
                "/api/dashboard/cash-flow" +
                $"?from={from:yyyy-MM-dd}" +
                $"&to={to:yyyy-MM-dd}");

        response.EnsureSuccessStatusCode();

        var cashFlow =
            await response.Content
                .ReadFromJsonAsync<
                    FinancialDashboardCashFlowResponse>();

        Assert.NotNull(cashFlow);

        var paymentDay =
            cashFlow.Items.Single(
                x =>
                    x.Date == from);

        var creationDay =
            cashFlow.Items.Single(
                x =>
                    x.Date == to);

        /*
         * El efectivo se agrupa por PaymentDate.
         */
        Assert.Equal(
            100m,
            paymentDay.CashCollected);

        Assert.Equal(
            100m,
            paymentDay.ContractualCashCollected);

        /*
         * El préstamo se agrupa por CreatedAt,
         * no por StartDate.
         */
        Assert.Equal(
            1000m,
            creationDay.PrincipalOriginated);
    }

    [Fact]
    public async Task CashFlow_InvalidRange_ReturnsBadRequest()
    {
        var context =
            await CreateContextAsync();

        var response =
            await context.Client.GetAsync(
                "/api/dashboard/cash-flow" +
                "?from=2026-09-10" +
                "&to=2026-09-01");

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task CashFlow_PeriodOver366Days_ReturnsBadRequest()
    {
        var context =
            await CreateContextAsync();

        var from =
            new DateOnly(
                2025,
                1,
                1);

        var to =
            from.AddDays(366);

        /*
         * Inclusivo:
         * 367 días.
         */
        var response =
            await context.Client.GetAsync(
                "/api/dashboard/cash-flow" +
                $"?from={from:yyyy-MM-dd}" +
                $"&to={to:yyyy-MM-dd}");

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    // ============================================================
    // AUTHORIZATION
    // ============================================================

    [Fact]
    public async Task Dashboard_WithoutToken_ReturnsUnauthorized()
    {
        var client =
            _factory.CreateClient();

        var response =
            await client.GetAsync(
                "/api/dashboard/summary");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task Dashboard_Collector_ReturnsForbidden()
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
            await collectorClient.GetAsync(
                "/api/dashboard/summary");

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    // ============================================================
    // SCENARIOS
    // ============================================================

    private async Task<LoanScenario>
        CreateLoanScenarioAsync(
            DateTime startDate,
            bool lateFeeEnabled = false,
            decimal lateFeeAmount = 0m)
    {
        var context =
            await CreateContextAsync();

        var investorId =
            await CreateInvestorAsync(
                context.Client);

        var clientId =
            await CreateClientAsync(
                context.Client);

        var loan =
            await CreateLoanAsync(
                context.Client,
                investorId,
                clientId,
                startDate,
                lateFeeEnabled,
                lateFeeAmount);

        return new LoanScenario(
            context,
            loan);
    }

    private sealed record LoanScenario(
        TestTenantContext Context,
        LoanResponse Loan);

    private sealed record CollectorCredentials(
        Guid Id,
        string Username,
        string Password);

    // ============================================================
    // AUTHENTICATION
    // ============================================================

    private async Task<TestTenantContext>
        CreateContextAsync()
    {
        return await TestAuthenticationHelper
            .CreateAdministratorContextAsync(
                _factory);
    }

    private async Task<CollectorCredentials>
        CreateCollectorAsync(
            TestTenantContext context)
    {
        var suffix =
            Guid.NewGuid()
                .ToString("N");

        var username =
            $"dashboard-collector-{suffix}";

        var password =
            TestAuthenticationHelper
                .DefaultPassword;

        var response =
            await context.Client.PostAsJsonAsync(
                "/api/app-users",
                new
                {
                    name =
                        $"Dashboard Collector {suffix}",

                    username,

                    password,

                    email =
                        $"dashboard-{suffix}@sanes.test",

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

    // ============================================================
    // API HELPERS
    // ============================================================

    private static async Task<Guid>
        CreateInvestorAsync(
            HttpClient client)
    {
        var request =
            new CreateInvestorRequest
            {
                Name =
                    $"Dashboard Investor {Guid.NewGuid():N}",

                Phone =
                    "8095551000",

                Identification =
                    $"DBI-{Guid.NewGuid():N}"
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

        return investor.Id;
    }

    private static async Task<Guid>
        CreateClientAsync(
            HttpClient client)
    {
        var request =
            new CreateClientRequest
            {
                FirstName =
                    "Dashboard",

                LastName =
                    $"Client {Guid.NewGuid():N}",

                Phone =
                    $"809{Random.Shared.Next(
                        1000000,
                        9999999)}",

                Address =
                    "Financial Dashboard Integration Test"
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

        return createdClient.Id;
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
        var request =
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
                    "Financial dashboard integration test",

                LateFeeEnabled =
                    lateFeeEnabled,

                LateFeeCalculationType =
                    LateFeeCalculationType
                        .FixedAmountPerInstallment,

                LateFeeAmount =
                    lateFeeAmount,

                LateFeeGraceDays =
                    0
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
                    "Financial dashboard integration test payment"
            });
    }

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
                        "Financial dashboard integration test"
                });

        response.EnsureSuccessStatusCode();

        var settlement =
            await response.Content
                .ReadFromJsonAsync<
                    EarlySettlementResponse>();

        Assert.NotNull(settlement);

        return settlement;
    }
}