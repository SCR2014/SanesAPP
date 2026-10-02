using System.Net;
using System.Net.Http.Json;
using Sanes.Api.IntegrationTests.Helpers;
using Sanes.Application.Clients.DTOs;
using Sanes.Application.Dashboard.DTOs;
using Sanes.Application.FinancialReports.DTOs;
using Sanes.Application.Investors.DTOs;
using Sanes.Application.LateFees.DTOs;
using Sanes.Application.Loans.DTOs;
using Sanes.Application.Payments.DTOs;
using Sanes.Domain.Enums;

namespace Sanes.Api.IntegrationTests.Payments;

public class PaymentReversalFinancialEffectsTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory
        _factory;

    public PaymentReversalFinancialEffectsTests(
        CustomWebApplicationFactory factory)
    {
        _factory =
            factory;
    }

    // ============================================================
    // LATE FEE + DASHBOARD SUMMARY
    // ============================================================

    [Fact]
    public async Task
        ReversePayment_WithLateFee_RestoresLateFeeAndDashboardSummary()
    {
        var scenario =
            await CreateScenarioAsync();

        /*
         * StartDate = hoy - 8.
         *
         * Weekly:
         * primera cuota vence ayer.
         *
         * Fixed late fee = 20.
         */
        var lateFeesBeforePayment =
            await GetLateFeesAsync(
                scenario.AdministratorClient,
                scenario.Loan.Id);

        var chargeBeforePayment =
            Assert.Single(
                lateFeesBeforePayment.Charges);

        Assert.Equal(
            20m,
            lateFeesBeforePayment.LateFeeBalance);

        Assert.Equal(
            20m,
            chargeBeforePayment.OutstandingAmount);

        /*
         * RD$120:
         *
         * 20  -> mora
         * 100 -> saldo contractual
         */
        var payment =
            await CreatePaymentAsync(
                scenario.AdministratorClient,
                scenario.Loan.Id,
                120m,
                DateTime.UtcNow,
                PaymentType.Regular);

        var lateFeesAfterPayment =
            await GetLateFeesAsync(
                scenario.AdministratorClient,
                scenario.Loan.Id);

        var chargeAfterPayment =
            Assert.Single(
                lateFeesAfterPayment.Charges);

        Assert.Equal(
            0m,
            lateFeesAfterPayment.LateFeeBalance);

        Assert.Equal(
            20m,
            chargeAfterPayment.PaidAmount);

        Assert.Equal(
            0m,
            chargeAfterPayment.OutstandingAmount);

        var dashboardAfterPayment =
            await GetDashboardSummaryAsync(
                scenario.AdministratorClient);

        Assert.Equal(
            120m,
            dashboardAfterPayment.CashCollected);

        Assert.Equal(
            100m,
            dashboardAfterPayment
                .ContractualCashCollected);

        Assert.Equal(
            20m,
            dashboardAfterPayment.LateFeesCollected);

        Assert.Equal(
            1200m,
            dashboardAfterPayment
                .ContractualBalanceOutstanding);

        Assert.Equal(
            0m,
            dashboardAfterPayment
                .LateFeeBalanceOutstanding);

        Assert.Equal(
            1200m,
            dashboardAfterPayment.TotalOutstanding);

        var reversalResponse =
            await ReverseAsync(
                scenario.AdministratorClient,
                payment.Id,
                "Corrección de pago con mora");

        Assert.Equal(
            HttpStatusCode.OK,
            reversalResponse.StatusCode);

        /*
         * Las allocations permanecen históricamente,
         * pero dejan de ser efectivas.
         *
         * Por tanto, los RD$20 aplicados a mora
         * vuelven a quedar pendientes.
         */
        var lateFeesAfterReversal =
            await GetLateFeesAsync(
                scenario.AdministratorClient,
                scenario.Loan.Id);

        var restoredCharge =
            Assert.Single(
                lateFeesAfterReversal.Charges);

        Assert.Equal(
            20m,
            lateFeesAfterReversal.LateFeeBalance);

        Assert.Equal(
            0m,
            restoredCharge.PaidAmount);

        Assert.Equal(
            20m,
            restoredCharge.OutstandingAmount);

        var dashboardAfterReversal =
            await GetDashboardSummaryAsync(
                scenario.AdministratorClient);

        /*
         * El pago reversado ya no representa
         * efectivo efectivo recibido.
         */
        Assert.Equal(
            0m,
            dashboardAfterReversal.CashCollected);

        Assert.Equal(
            0m,
            dashboardAfterReversal
                .ContractualCashCollected);

        Assert.Equal(
            0m,
            dashboardAfterReversal.LateFeesCollected);

        /*
         * La deuda vuelve al estado anterior al pago:
         *
         * Contractual = 1300
         * Mora        =   20
         * Total       = 1320
         */
        Assert.Equal(
            1300m,
            dashboardAfterReversal
                .ContractualBalanceOutstanding);

        Assert.Equal(
            20m,
            dashboardAfterReversal
                .LateFeeBalanceOutstanding);

        Assert.Equal(
            1320m,
            dashboardAfterReversal.TotalOutstanding);
    }

    // ============================================================
    // CASH FLOW + COLLECTIONS REPORT
    // ============================================================

    [Fact]
    public async Task
        ReversePayment_IsExcludedFromCashFlowAndCollectionsReport()
    {
        var scenario =
            await CreateScenarioAsync();

        var now =
            DateTime.UtcNow;

        var date =
            DateOnly.FromDateTime(
                now);

        /*
         * Materializamos la mora antes del pago.
         */
        var lateFees =
            await GetLateFeesAsync(
                scenario.AdministratorClient,
                scenario.Loan.Id);

        Assert.Equal(
            20m,
            lateFees.LateFeeBalance);

        var payment =
            await CreatePaymentAsync(
                scenario.AdministratorClient,
                scenario.Loan.Id,
                120m,
                now,
                PaymentType.Regular);

        var cashFlowBefore =
            await GetCashFlowAsync(
                scenario.AdministratorClient,
                date,
                date);

        var cashFlowDayBefore =
            Assert.Single(
                cashFlowBefore.Items,
                x => x.Date == date);

        Assert.Equal(
            120m,
            cashFlowBefore.CashCollected);

        Assert.Equal(
            100m,
            cashFlowBefore.ContractualCashCollected);

        Assert.Equal(
            20m,
            cashFlowBefore.LateFeesCollected);

        Assert.Equal(
            120m,
            cashFlowDayBefore.CashCollected);

        Assert.Equal(
            100m,
            cashFlowDayBefore.ContractualCashCollected);

        Assert.Equal(
            20m,
            cashFlowDayBefore.LateFeesCollected);

        var collectionsBefore =
            await GetCollectionsAsync(
                scenario.AdministratorClient,
                date);

        Assert.Equal(
            1,
            collectionsBefore.Summary.PaymentsCount);

        Assert.Equal(
            1,
            collectionsBefore.Summary.ClientsCount);

        Assert.Equal(
            120m,
            collectionsBefore.Summary.CashCollected);

        Assert.Equal(
            100m,
            collectionsBefore.Summary
                .ContractualCashCollected);

        Assert.Equal(
            20m,
            collectionsBefore.Summary
                .LateFeesCollected);

        var collectionItem =
            Assert.Single(
                collectionsBefore.Items);

        Assert.Equal(
            payment.Id,
            collectionItem.PaymentId);

        Assert.Equal(
            120m,
            collectionItem.CashCollected);

        Assert.Equal(
            100m,
            collectionItem.ContractualCashCollected);

        Assert.Equal(
            20m,
            collectionItem.LateFeesCollected);

        var reversalResponse =
            await ReverseAsync(
                scenario.AdministratorClient,
                payment.Id,
                "Excluir pago reversado de métricas históricas");

        Assert.Equal(
            HttpStatusCode.OK,
            reversalResponse.StatusCode);

        /*
         * Cash Flow conserva la fecha dentro de la serie,
         * pero el pago ya no aporta efectivo.
         */
        var cashFlowAfter =
            await GetCashFlowAsync(
                scenario.AdministratorClient,
                date,
                date);

        var cashFlowDayAfter =
            Assert.Single(
                cashFlowAfter.Items,
                x => x.Date == date);

        Assert.Equal(
            0m,
            cashFlowAfter.CashCollected);

        Assert.Equal(
            0m,
            cashFlowAfter.ContractualCashCollected);

        Assert.Equal(
            0m,
            cashFlowAfter.LateFeesCollected);

        Assert.Equal(
            0m,
            cashFlowDayAfter.CashCollected);

        Assert.Equal(
            0m,
            cashFlowDayAfter.ContractualCashCollected);

        Assert.Equal(
            0m,
            cashFlowDayAfter.LateFeesCollected);

        /*
         * Collections representa cobros efectivos.
         *
         * El Payment permanece en auditoría, pero ya
         * no debe aparecer en este reporte financiero.
         */
        var collectionsAfter =
            await GetCollectionsAsync(
                scenario.AdministratorClient,
                date);

        Assert.Equal(
            0,
            collectionsAfter.Summary.PaymentsCount);

        Assert.Equal(
            0,
            collectionsAfter.Summary.ClientsCount);

        Assert.Equal(
            0m,
            collectionsAfter.Summary.CashCollected);

        Assert.Equal(
            0m,
            collectionsAfter.Summary
                .ContractualCashCollected);

        Assert.Equal(
            0m,
            collectionsAfter.Summary
                .LateFeesCollected);

        Assert.Empty(
            collectionsAfter.Items);
    }

    // ============================================================
    // INVESTOR STATEMENT
    // ============================================================

    [Fact]
    public async Task
        ReversePayment_IsExcludedFromInvestorStatementHistoricalCash()
    {
        var scenario =
            await CreateScenarioAsync();

        var lateFees =
            await GetLateFeesAsync(
                scenario.AdministratorClient,
                scenario.Loan.Id);

        Assert.Equal(
            20m,
            lateFees.LateFeeBalance);

        var payment =
            await CreatePaymentAsync(
                scenario.AdministratorClient,
                scenario.Loan.Id,
                120m,
                DateTime.UtcNow,
                PaymentType.Regular);

        var statementAfterPayment =
            await GetInvestorStatementAsync(
                scenario.AdministratorClient,
                scenario.InvestorId);

        Assert.Equal(
            120m,
            statementAfterPayment.CashCollected);

        Assert.Equal(
            100m,
            statementAfterPayment
                .ContractualCashCollected);

        Assert.Equal(
            20m,
            statementAfterPayment.LateFeesCollected);

        Assert.Equal(
            1200m,
            statementAfterPayment
                .ContractualBalanceOutstanding);

        Assert.Equal(
            0m,
            statementAfterPayment
                .LateFeeBalanceOutstanding);

        Assert.Equal(
            1200m,
            statementAfterPayment.TotalOutstanding);

        var reversalResponse =
            await ReverseAsync(
                scenario.AdministratorClient,
                payment.Id,
                "Corrección para estado del inversionista");

        Assert.Equal(
            HttpStatusCode.OK,
            reversalResponse.StatusCode);

        var statementAfterReversal =
            await GetInvestorStatementAsync(
                scenario.AdministratorClient,
                scenario.InvestorId);

        /*
         * La originación histórica sigue existiendo,
         * solamente desaparece el cobro reversado.
         */
        Assert.Equal(
            1000m,
            statementAfterReversal.PrincipalOriginated);

        Assert.Equal(
            1300m,
            statementAfterReversal
                .ContractualAmountOriginated);

        Assert.Equal(
            0m,
            statementAfterReversal.CashCollected);

        Assert.Equal(
            0m,
            statementAfterReversal
                .ContractualCashCollected);

        Assert.Equal(
            0m,
            statementAfterReversal.LateFeesCollected);

        Assert.Equal(
            1,
            statementAfterReversal.ActiveLoansCount);

        Assert.Equal(
            1,
            statementAfterReversal.ActiveClientsCount);

        Assert.Equal(
            1300m,
            statementAfterReversal
                .ContractualBalanceOutstanding);

        Assert.Equal(
            20m,
            statementAfterReversal
                .LateFeeBalanceOutstanding);

        Assert.Equal(
            1320m,
            statementAfterReversal.TotalOutstanding);

        var activeLoan =
            Assert.Single(
                statementAfterReversal.ActiveLoans);

        Assert.Equal(
            scenario.Loan.Id,
            activeLoan.LoanId);

        Assert.Equal(
            1300m,
            activeLoan.ContractualBalanceOutstanding);

        Assert.Equal(
            20m,
            activeLoan.LateFeeBalanceOutstanding);

        Assert.Equal(
            1320m,
            activeLoan.TotalOutstanding);
    }

    // ============================================================
    // SCENARIO
    // ============================================================

    private async Task<FinancialReversalScenario>
        CreateScenarioAsync()
    {
        var context =
            await TestAuthenticationHelper
                .CreateAdministratorContextAsync(
                    _factory);

        var investor =
            await CreateInvestorAsync(
                context.Client);

        var client =
            await CreateClientAsync(
                context.Client);

        var loan =
            await CreateLoanAsync(
                context.Client,
                investor.Id,
                client.Id,
                DateTime.UtcNow.Date.AddDays(-8));

        return new FinancialReversalScenario(
            context.Client,
            investor.Id,
            loan);
    }

    private static async Task<InvestorResponse>
        CreateInvestorAsync(
            HttpClient client)
    {
        var response =
            await client.PostAsJsonAsync(
                "/api/investors",
                new CreateInvestorRequest
                {
                    Name =
                        $"Financial Reversal Investor {Guid.NewGuid():N}",

                    Phone =
                        "8095551000",

                    Identification =
                        $"FIN-REV-{Guid.NewGuid():N}"
                });

        response.EnsureSuccessStatusCode();

        var investor =
            await response.Content
                .ReadFromJsonAsync<
                    InvestorResponse>();

        Assert.NotNull(
            investor);

        return investor;
    }

    private static async Task<ClientResponse>
        CreateClientAsync(
            HttpClient client)
    {
        var response =
            await client.PostAsJsonAsync(
                "/api/clients",
                new CreateClientRequest
                {
                    FirstName =
                        "Financial",

                    LastName =
                        $"Reversal-{Guid.NewGuid():N}",

                    Phone =
                        $"809{Random.Shared.Next(
                            1000000,
                            9999999)}",

                    Address =
                        "Payment reversal financial effects test"
                });

        response.EnsureSuccessStatusCode();

        var createdClient =
            await response.Content
                .ReadFromJsonAsync<
                    ClientResponse>();

        Assert.NotNull(
            createdClient);

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
                        "Payment reversal financial effects test",

                    LateFeeEnabled =
                        true,

                    LateFeeCalculationType =
                        LateFeeCalculationType
                            .FixedAmountPerInstallment,

                    LateFeeAmount =
                        20m,

                    LateFeeGraceDays =
                        0
                });

        response.EnsureSuccessStatusCode();

        var loan =
            await response.Content
                .ReadFromJsonAsync<
                    LoanResponse>();

        Assert.NotNull(
            loan);

        return loan;
    }

    // ============================================================
    // PAYMENT / REVERSAL
    // ============================================================

    private static async Task<PaymentResponse>
        CreatePaymentAsync(
            HttpClient client,
            Guid loanId,
            decimal amount,
            DateTime paymentDate,
            PaymentType paymentType)
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
                        paymentType,

                    Notes =
                        "Payment reversal financial effects test"
                });

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var payment =
            await response.Content
                .ReadFromJsonAsync<
                    PaymentResponse>();

        Assert.NotNull(
            payment);

        return payment;
    }

    private static Task<HttpResponseMessage>
        ReverseAsync(
            HttpClient client,
            Guid paymentId,
            string reason)
    {
        return client.PostAsJsonAsync(
            $"/api/payments/{paymentId}/reversal",
            new PaymentReversalRequest
            {
                Reason =
                    reason
            });
    }

    // ============================================================
    // FINANCIAL READS
    // ============================================================

    private static async Task<LateFeeLoanResponse>
        GetLateFeesAsync(
            HttpClient client,
            Guid loanId)
    {
        var response =
            await client.GetAsync(
                $"/api/late-fees/loans/{loanId}");

        response.EnsureSuccessStatusCode();

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    LateFeeLoanResponse>();

        Assert.NotNull(
            result);

        return result;
    }

    private static async Task<
        FinancialDashboardSummaryResponse>
        GetDashboardSummaryAsync(
            HttpClient client)
    {
        var response =
            await client.GetAsync(
                "/api/dashboard/summary");

        response.EnsureSuccessStatusCode();

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    FinancialDashboardSummaryResponse>();

        Assert.NotNull(
            result);

        return result;
    }

    private static async Task<
        FinancialDashboardCashFlowResponse>
        GetCashFlowAsync(
            HttpClient client,
            DateOnly from,
            DateOnly to)
    {
        var response =
            await client.GetAsync(
                "/api/dashboard/cash-flow" +
                $"?from={from:yyyy-MM-dd}" +
                $"&to={to:yyyy-MM-dd}");

        response.EnsureSuccessStatusCode();

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    FinancialDashboardCashFlowResponse>();

        Assert.NotNull(
            result);

        return result;
    }

    private static async Task<
        FinancialCollectionsReportResponse>
        GetCollectionsAsync(
            HttpClient client,
            DateOnly date)
    {
        var response =
            await client.GetAsync(
                "/api/reports/collections" +
                $"?from={date:yyyy-MM-dd}" +
                $"&to={date:yyyy-MM-dd}");

        response.EnsureSuccessStatusCode();

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    FinancialCollectionsReportResponse>();

        Assert.NotNull(
            result);

        return result;
    }

    private static async Task<
        FinancialInvestorStatementResponse>
        GetInvestorStatementAsync(
            HttpClient client,
            Guid investorId)
    {
        var response =
            await client.GetAsync(
                $"/api/reports/investors/{investorId}");

        response.EnsureSuccessStatusCode();

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    FinancialInvestorStatementResponse>();

        Assert.NotNull(
            result);

        return result;
    }

    private sealed record FinancialReversalScenario(
        HttpClient AdministratorClient,
        Guid InvestorId,
        LoanResponse Loan);
}