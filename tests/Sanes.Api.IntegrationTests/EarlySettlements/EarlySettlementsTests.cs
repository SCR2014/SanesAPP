using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sanes.Api.IntegrationTests.Helpers;
using Sanes.Application.Authentication.DTOs;
using Sanes.Application.Clients.DTOs;
using Sanes.Application.Investors.DTOs;
using Sanes.Application.Loans.DTOs;
using Sanes.Application.Payments.DTOs;
using Sanes.Domain.Enums;
using Sanes.Infrastructure.Persistence;

namespace Sanes.Api.IntegrationTests.EarlySettlements;

public class EarlySettlementsTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public EarlySettlementsTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ============================================================
    // QUOTE - ELIGIBILITY AND CALCULATION
    // ============================================================

    [Fact]
    public async Task Quote_WithFiveCompletedInstallments_ReturnsBadRequest()
    {
        var scenario =
            await CreateScenarioAsync();

        await PayInstallmentsAsync(
            scenario.Context.Client,
            scenario.Loan.Id,
            count: 5);

        var response =
            await scenario.Context.Client.PostAsJsonAsync(
                $"/api/loans/{scenario.Loan.Id}/early-settlement/quote",
                new EarlySettlementQuoteRequest
                {
                    DiscountType =
                        EarlySettlementDiscountType
                            .InstallmentWaiver,

                    DiscountValue =
                        1
                });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task Quote_WithExactlySixCompletedInstallments_ReturnsExpectedInstallmentWaiver()
    {
        var scenario =
            await CreateEligibleScenarioAsync();

        var quote =
            await QuoteAsync(
                scenario.Context.Client,
                scenario.Loan.Id,
                EarlySettlementDiscountType
                    .InstallmentWaiver,
                2);

        /*
         * Total contractual = 13 x 100 = 1300
         * Cash paid         = 600
         * Balance           = 700
         * Discount          = 2 x 100 = 200
         * Settlement        = 500
         */
        Assert.True(
            quote.IsEligible);

        Assert.Equal(
            6,
            quote.CompletedInstallments);

        Assert.Equal(
            600m,
            quote.AmountPaid);

        Assert.Equal(
            700m,
            quote.ContractualBalance);

        Assert.Equal(
            0m,
            quote.LateFeeBalance);

        Assert.Equal(
            200m,
            quote.DiscountAmount);

        Assert.Equal(
            500m,
            quote.SettlementAmount);
    }

    [Fact]
    public async Task Quote_WithPercentageDiscount_CalculatesDiscountFromContractualBalance()
    {
        var scenario =
            await CreateEligibleScenarioAsync();

        var quote =
            await QuoteAsync(
                scenario.Context.Client,
                scenario.Loan.Id,
                EarlySettlementDiscountType
                    .PercentageDiscount,
                15);

        /*
         * Contractual balance = 700
         * 15%                 = 105
         * Settlement          = 595
         */
        Assert.Equal(
            700m,
            quote.ContractualBalance);

        Assert.Equal(
            105m,
            quote.DiscountAmount);

        Assert.Equal(
            595m,
            quote.SettlementAmount);
    }

    [Fact]
    public async Task Quote_WithFourInstallmentWaiver_ReturnsBadRequest()
    {
        var scenario =
            await CreateEligibleScenarioAsync();

        var response =
            await scenario.Context.Client.PostAsJsonAsync(
                $"/api/loans/{scenario.Loan.Id}/early-settlement/quote",
                new EarlySettlementQuoteRequest
                {
                    DiscountType =
                        EarlySettlementDiscountType
                            .InstallmentWaiver,

                    DiscountValue =
                        4
                });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task Quote_WithPercentageGreaterThan100_ReturnsBadRequest()
    {
        var scenario =
            await CreateEligibleScenarioAsync();

        var response =
            await scenario.Context.Client.PostAsJsonAsync(
                $"/api/loans/{scenario.Loan.Id}/early-settlement/quote",
                new EarlySettlementQuoteRequest
                {
                    DiscountType =
                        EarlySettlementDiscountType
                            .PercentageDiscount,

                    DiscountValue =
                        100.01m
                });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    // ============================================================
    // AUTHORIZATION AND TENANT ISOLATION
    // ============================================================

    [Fact]
    public async Task Quote_CannotAccessLoanFromAnotherTenant()
    {
        var tenant1 =
            await CreateContextAsync();

        var tenant2Scenario =
            await CreateEligibleScenarioAsync();

        var response =
            await tenant1.Client.PostAsJsonAsync(
                $"/api/loans/{tenant2Scenario.Loan.Id}/early-settlement/quote",
                new EarlySettlementQuoteRequest
                {
                    DiscountType =
                        EarlySettlementDiscountType
                            .InstallmentWaiver,

                    DiscountValue =
                        1
                });

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task Quote_WithCollector_ReturnsForbidden()
    {
        var scenario =
            await CreateEligibleScenarioAsync();

        var collector =
            await CreateCollectorAsync(
                scenario.Context);

        var collectorClient =
            await LoginCollectorAsync(
                scenario.Context.TenantId,
                collector);

        var response =
            await collectorClient.PostAsJsonAsync(
                $"/api/loans/{scenario.Loan.Id}/early-settlement/quote",
                new EarlySettlementQuoteRequest
                {
                    DiscountType =
                        EarlySettlementDiscountType
                            .InstallmentWaiver,

                    DiscountValue =
                        1
                });

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    // ============================================================
    // HISTORICAL QUERY
    // ============================================================

    [Fact]
    public async Task GetByLoan_BeforeSettlement_ReturnsNotFound()
    {
        var scenario =
            await CreateEligibleScenarioAsync();

        var response =
            await scenario.Context.Client.GetAsync(
                $"/api/loans/{scenario.Loan.Id}/early-settlement");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    // ============================================================
    // EXECUTION
    // ============================================================

    [Fact]
    public async Task Execute_WithStaleExpectedAmount_ReturnsBadRequestAndPersistsNothing()
    {
        var scenario =
            await CreateEligibleScenarioAsync();

        var quote =
            await QuoteAsync(
                scenario.Context.Client,
                scenario.Loan.Id,
                EarlySettlementDiscountType
                    .InstallmentWaiver,
                2);

        Assert.Equal(
            500m,
            quote.SettlementAmount);

        /*
         * Modificamos el saldo después de cotizar.
         *
         * Ahora:
         * CashPaid = 650
         * Balance  = 650
         * Discount = 200
         * Current settlement = 450
         *
         * El cliente todavía intentará confirmar 500.
         */
        var paymentResponse =
            await CreatePaymentAsync(
                scenario.Context.Client,
                scenario.Loan.Id,
                50m,
                DateTime.UtcNow,
                PaymentType.Partial);

        Assert.Equal(
            HttpStatusCode.Created,
            paymentResponse.StatusCode);

        var executeResponse =
            await scenario.Context.Client.PostAsJsonAsync(
                $"/api/loans/{scenario.Loan.Id}/early-settlement",
                new EarlySettlementExecuteRequest
                {
                    DiscountType =
                        EarlySettlementDiscountType
                            .InstallmentWaiver,

                    DiscountValue =
                        2,

                    ExpectedSettlementAmount =
                        quote.SettlementAmount,

                    Reason =
                        "Stale quote integration test"
                });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            executeResponse.StatusCode);

        await using var scope =
            _factory.Services.CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<SanesDbContext>();

        var settlementExists =
            await db.EarlySettlements
                .AsNoTracking()
                .AnyAsync(
                    x =>
                        x.TenantId ==
                            scenario.Context.TenantId &&
                        x.LoanId ==
                            scenario.Loan.Id);

        var adjustmentExists =
            await db.LoanBalanceAdjustments
                .AsNoTracking()
                .AnyAsync(
                    x =>
                        x.TenantId ==
                            scenario.Context.TenantId &&
                        x.LoanId ==
                            scenario.Loan.Id);

        Assert.False(
            settlementExists);

        Assert.False(
            adjustmentExists);
    }

    [Fact]
    public async Task Execute_WithInstallmentWaiver_CreatesSettlementAdjustmentAndMarksLoanPaid()
    {
        var scenario =
            await CreateEligibleScenarioAsync();

        var quote =
            await QuoteAsync(
                scenario.Context.Client,
                scenario.Loan.Id,
                EarlySettlementDiscountType
                    .InstallmentWaiver,
                2);

        var settlement =
            await ExecuteAsync(
                scenario.Context.Client,
                scenario.Loan.Id,
                EarlySettlementDiscountType
                    .InstallmentWaiver,
                2,
                quote.SettlementAmount);

        Assert.NotEqual(
            Guid.Empty,
            settlement.Id);

        Assert.NotNull(
            settlement.PaymentId);

        Assert.NotEqual(
            Guid.Empty,
            settlement.LoanBalanceAdjustmentId);

        Assert.Equal(
            700m,
            settlement.ContractualBalanceBefore);

        Assert.Equal(
            200m,
            settlement.DiscountAmount);

        Assert.Equal(
            500m,
            settlement.SettlementAmount);

        Assert.Equal(
            500m,
            settlement.AppliedToLoan);

        Assert.Equal(
            0m,
            settlement.AppliedToLateFees);

        Assert.Equal(
            0m,
            settlement.ContractualBalanceAfter);

        Assert.Equal(
            0m,
            settlement.LateFeeBalanceAfter);

        Assert.Equal(
            0m,
            settlement.TotalOutstandingAfter);

        await using var scope =
            _factory.Services.CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<SanesDbContext>();

        var persistedSettlement =
            await db.EarlySettlements
                .AsNoTracking()
                .SingleAsync(
                    x =>
                        x.TenantId ==
                            scenario.Context.TenantId &&
                        x.LoanId ==
                            scenario.Loan.Id);

        Assert.Equal(
            settlement.Id,
            persistedSettlement.Id);

        Assert.Equal(
            200m,
            persistedSettlement.DiscountAmount);

        var adjustment =
            await db.LoanBalanceAdjustments
                .AsNoTracking()
                .SingleAsync(
                    x =>
                        x.Id ==
                        settlement.LoanBalanceAdjustmentId);

        Assert.Equal(
            LoanBalanceAdjustmentType
                .EarlySettlementDiscount,
            adjustment.AdjustmentType);

        Assert.Equal(
            200m,
            adjustment.Amount);

        var loan =
            await db.Loans
                .AsNoTracking()
                .SingleAsync(
                    x =>
                        x.Id ==
                        scenario.Loan.Id);

        Assert.Equal(
            LoanStatus.Paid,
            loan.Status);
    }

    [Fact]
    public async Task Execute_WithLateFee_AppliesSettlementPaymentToLateFeeFirst()
    {
        var scenario =
            await CreateEligibleLateFeeScenarioAsync();

        var quote =
            await QuoteAsync(
                scenario.Context.Client,
                scenario.Loan.Id,
                EarlySettlementDiscountType
                    .InstallmentWaiver,
                2);

        /*
         * First six installments were paid on time.
         *
         * Installment #7:
         * Due       = yesterday
         * Effective = today
         *
         * Contractual balance = 700
         * Late fee            = 20
         * Discount            = 200
         * Settlement          = 520
         */
        Assert.Equal(
            700m,
            quote.ContractualBalance);

        Assert.Equal(
            20m,
            quote.LateFeeBalance);

        Assert.Equal(
            200m,
            quote.DiscountAmount);

        Assert.Equal(
            520m,
            quote.SettlementAmount);

        var settlement =
            await ExecuteAsync(
                scenario.Context.Client,
                scenario.Loan.Id,
                EarlySettlementDiscountType
                    .InstallmentWaiver,
                2,
                quote.SettlementAmount);

        Assert.NotNull(
            settlement.PaymentId);

        Assert.Equal(
            20m,
            settlement.AppliedToLateFees);

        Assert.Equal(
            500m,
            settlement.AppliedToLoan);

        Assert.Equal(
            0m,
            settlement.TotalOutstandingAfter);

        await using var scope =
            _factory.Services.CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<SanesDbContext>();

        var allocations =
            await db.PaymentAllocations
                .AsNoTracking()
                .Where(
                    x =>
                        x.PaymentId ==
                        settlement.PaymentId!.Value)
                .ToListAsync();

        Assert.Equal(
            2,
            allocations.Count);

        Assert.Contains(
            allocations,
            x =>
                x.AllocationType ==
                    PaymentAllocationType.LateFee &&
                x.Amount ==
                    20m);

        Assert.Contains(
            allocations,
            x =>
                x.AllocationType ==
                    PaymentAllocationType.LoanBalance &&
                x.Amount ==
                    500m);
    }

    [Fact]
    public async Task Execute_With100PercentDiscountAndNoLateFees_DoesNotCreateFakePayment()
    {
        var scenario =
            await CreateEligibleScenarioAsync();

        var quote =
            await QuoteAsync(
                scenario.Context.Client,
                scenario.Loan.Id,
                EarlySettlementDiscountType
                    .PercentageDiscount,
                100);

        Assert.Equal(
            700m,
            quote.ContractualBalance);

        Assert.Equal(
            700m,
            quote.DiscountAmount);

        Assert.Equal(
            0m,
            quote.SettlementAmount);

        var settlement =
            await ExecuteAsync(
                scenario.Context.Client,
                scenario.Loan.Id,
                EarlySettlementDiscountType
                    .PercentageDiscount,
                100,
                0m);

        Assert.Null(
            settlement.PaymentId);

        Assert.Equal(
            700m,
            settlement.DiscountAmount);

        Assert.Equal(
            0m,
            settlement.SettlementAmount);

        Assert.Equal(
            0m,
            settlement.TotalOutstandingAfter);

        await using var scope =
            _factory.Services.CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<SanesDbContext>();

        var persisted =
            await db.EarlySettlements
                .AsNoTracking()
                .SingleAsync(
                    x =>
                        x.Id ==
                        settlement.Id);

        Assert.Null(
            persisted.PaymentId);

        var loan =
            await db.Loans
                .AsNoTracking()
                .SingleAsync(
                    x =>
                        x.Id ==
                        scenario.Loan.Id);

        Assert.Equal(
            LoanStatus.Paid,
            loan.Status);
    }

    [Fact]
    public async Task Execute_DiscountDoesNotInflateCashAppliedToLoan()
    {
        var scenario =
            await CreateEligibleScenarioAsync();

        var quote =
            await QuoteAsync(
                scenario.Context.Client,
                scenario.Loan.Id,
                EarlySettlementDiscountType
                    .InstallmentWaiver,
                2);

        await ExecuteAsync(
            scenario.Context.Client,
            scenario.Loan.Id,
            EarlySettlementDiscountType
                .InstallmentWaiver,
            2,
            quote.SettlementAmount);

        await using var scope =
            _factory.Services.CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<SanesDbContext>();

        /*
         * Six previous installments:
         * 600 cash applied.
         *
         * Settlement cash:
         * 500 applied to loan.
         *
         * Total cash applied to loan = 1100.
         *
         * The remaining 200 are NOT cash;
         * they are the contractual adjustment.
         */
        var cashAppliedToLoan =
            await db.PaymentAllocations
                .AsNoTracking()
                .Where(
                    x =>
                        x.TenantId ==
                            scenario.Context.TenantId &&
                        x.Payment.LoanId ==
                            scenario.Loan.Id &&
                        x.AllocationType ==
                            PaymentAllocationType
                                .LoanBalance)
                .SumAsync(
                    x => x.Amount);

        var contractualAdjustments =
            await db.LoanBalanceAdjustments
                .AsNoTracking()
                .Where(
                    x =>
                        x.TenantId ==
                            scenario.Context.TenantId &&
                        x.LoanId ==
                            scenario.Loan.Id)
                .SumAsync(
                    x => x.Amount);

        Assert.Equal(
            1100m,
            cashAppliedToLoan);

        Assert.Equal(
            200m,
            contractualAdjustments);

        Assert.Equal(
            1300m,
            cashAppliedToLoan +
            contractualAdjustments);
    }

    [Fact]
    public async Task Execute_SecondSettlementAttempt_ReturnsBadRequest()
    {
        var scenario =
            await CreateEligibleScenarioAsync();

        var quote =
            await QuoteAsync(
                scenario.Context.Client,
                scenario.Loan.Id,
                EarlySettlementDiscountType
                    .InstallmentWaiver,
                2);

        await ExecuteAsync(
            scenario.Context.Client,
            scenario.Loan.Id,
            EarlySettlementDiscountType
                .InstallmentWaiver,
            2,
            quote.SettlementAmount);

        var secondResponse =
            await scenario.Context.Client.PostAsJsonAsync(
                $"/api/loans/{scenario.Loan.Id}/early-settlement",
                new EarlySettlementExecuteRequest
                {
                    DiscountType =
                        EarlySettlementDiscountType
                            .InstallmentWaiver,

                    DiscountValue =
                        2,

                    ExpectedSettlementAmount =
                        quote.SettlementAmount,

                    Reason =
                        "Second settlement attempt"
                });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            secondResponse.StatusCode);

        await using var scope =
            _factory.Services.CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<SanesDbContext>();

        var count =
            await db.EarlySettlements
                .AsNoTracking()
                .CountAsync(
                    x =>
                        x.TenantId ==
                            scenario.Context.TenantId &&
                        x.LoanId ==
                            scenario.Loan.Id);

        Assert.Equal(
            1,
            count);
    }

    [Fact]
    public async Task GetByLoan_AfterSettlement_ReturnsHistoricalSnapshot()
    {
        var scenario =
            await CreateEligibleScenarioAsync();

        var quote =
            await QuoteAsync(
                scenario.Context.Client,
                scenario.Loan.Id,
                EarlySettlementDiscountType
                    .PercentageDiscount,
                10);

        /*
         * Balance = 700
         * 10%     = 70
         * Pay     = 630
         */
        var executed =
            await ExecuteAsync(
                scenario.Context.Client,
                scenario.Loan.Id,
                EarlySettlementDiscountType
                    .PercentageDiscount,
                10,
                quote.SettlementAmount);

        var response =
            await scenario.Context.Client.GetAsync(
                $"/api/loans/{scenario.Loan.Id}/early-settlement");

        response.EnsureSuccessStatusCode();

        var historical =
            await response.Content
                .ReadFromJsonAsync<
                    EarlySettlementResponse>();

        Assert.NotNull(
            historical);

        Assert.Equal(
            executed.Id,
            historical.Id);

        Assert.Equal(
            scenario.Loan.Id,
            historical.LoanId);

        Assert.Equal(
            700m,
            historical.ContractualBalanceBefore);

        Assert.Equal(
            0m,
            historical.LateFeeBalanceBefore);

        Assert.Equal(
            700m,
            historical.TotalOutstandingBefore);

        Assert.Equal(
            EarlySettlementDiscountType
                .PercentageDiscount,
            historical.DiscountType);

        Assert.Equal(
            10m,
            historical.DiscountValue);

        Assert.Equal(
            70m,
            historical.DiscountAmount);

        Assert.Equal(
            630m,
            historical.SettlementAmount);

        Assert.Equal(
            630m,
            historical.AppliedToLoan);

        Assert.Equal(
            0m,
            historical.AppliedToLateFees);

        Assert.Equal(
            0m,
            historical.TotalOutstandingAfter);
    }

    [Fact]
    public async Task Execute_WithLateFeeAndDiscount_CreatesCorrectPaymentReceipt()
    {
        var scenario =
            await CreateEligibleLateFeeScenarioAsync();

        var quote =
            await QuoteAsync(
                scenario.Context.Client,
                scenario.Loan.Id,
                EarlySettlementDiscountType
                    .InstallmentWaiver,
                2);

        /*
        * Contractual balance = 700
        * Mora                =  20
        * Descuento           = 200
        *
        * Cash settlement:
        *
        * 20  -> mora
        * 500 -> préstamo
        *
        * Efectivo real = 520
        */
        Assert.Equal(
            700m,
            quote.ContractualBalance);

        Assert.Equal(
            20m,
            quote.LateFeeBalance);

        Assert.Equal(
            200m,
            quote.DiscountAmount);

        Assert.Equal(
            520m,
            quote.SettlementAmount);

        var settlement =
            await ExecuteAsync(
                scenario.Context.Client,
                scenario.Loan.Id,
                EarlySettlementDiscountType
                    .InstallmentWaiver,
                2,
                quote.SettlementAmount);

        Assert.NotNull(
            settlement.PaymentId);

        var receiptResponse =
            await scenario.Context.Client.GetAsync(
                $"/api/payments/{settlement.PaymentId.Value}/receipt");

        Assert.Equal(
            HttpStatusCode.OK,
            receiptResponse.StatusCode);

        var receipt =
            await receiptResponse.Content
                .ReadFromJsonAsync<
                    PaymentReceiptResponse>();

        Assert.NotNull(receipt);

        /*
        * El descuento de RD$200 NO es efectivo.
        */
        Assert.Equal(
            520m,
            receipt.AmountReceived);

        Assert.Equal(
            20m,
            receipt.LateFeeAmountApplied);

        Assert.Equal(
            500m,
            receipt.LoanBalanceAmountApplied);

        /*
        * El descuento contractual + efectivo
        * dejan el préstamo completamente liquidado.
        */
        Assert.Equal(
            0m,
            receipt.ContractualBalanceAfter);

        Assert.Equal(
            0m,
            receipt.LateFeeBalanceAfter);

        Assert.Equal(
            0m,
            receipt.TotalOutstandingAfter);

        Assert.Equal(
            PaymentType.FullSettlement,
            receipt.PaymentType);

        Assert.Equal(
            "Early settlement",
            receipt.Notes);

        /*
        * Confirmación explícita:
        *
        * DiscountAmount no fue incorporado
        * al dinero recibido.
        */
        Assert.Equal(
            200m,
            settlement.DiscountAmount);

        Assert.NotEqual(
            settlement.SettlementAmount +
                settlement.DiscountAmount,
            receipt.AmountReceived);
    }

    // ============================================================
    // SCENARIOS
    // ============================================================

    private async Task<SettlementScenario>
        CreateScenarioAsync(
            DateTime? startDate = null,
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
                startDate ??
                    DateTime.UtcNow.Date,
                lateFeeEnabled,
                lateFeeAmount);

        return new SettlementScenario(
            context,
            loan);
    }

    private async Task<SettlementScenario>
        CreateEligibleScenarioAsync()
    {
        var scenario =
            await CreateScenarioAsync();

        await PayInstallmentsAsync(
            scenario.Context.Client,
            scenario.Loan.Id,
            count: 6);

        return scenario;
    }

    private async Task<SettlementScenario>
        CreateEligibleLateFeeScenarioAsync()
    {
        var today =
            DateTime.UtcNow.Date;

        /*
         * First due date = StartDate + 7 days.
         *
         * StartDate = today - 50:
         *
         * Installment #1 due = today - 43
         * #2 = -36
         * #3 = -29
         * #4 = -22
         * #5 = -15
         * #6 = -8
         * #7 = -1
         *
         * With graceDays = 0:
         * #7 effective date = today.
         */
        var startDate =
            today.AddDays(-50);

        var scenario =
            await CreateScenarioAsync(
                startDate,
                lateFeeEnabled: true,
                lateFeeAmount: 20m);

        /*
         * Pay the first six installments on their
         * respective due dates so that they do not
         * generate late fees.
         */
        for (var installment = 1;
             installment <= 6;
             installment++)
        {
            var paymentDate =
                startDate.AddDays(
                    installment * 7);

            var response =
                await CreatePaymentAsync(
                    scenario.Context.Client,
                    scenario.Loan.Id,
                    100m,
                    paymentDate,
                    PaymentType.Regular);

            Assert.Equal(
                HttpStatusCode.Created,
                response.StatusCode);
        }

        return scenario;
    }

    private sealed record SettlementScenario(
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
            $"settlement-collector-{suffix}";

        var password =
            TestAuthenticationHelper
                .DefaultPassword;

        var response =
            await context.Client.PostAsJsonAsync(
                "/api/app-users",
                new
                {
                    name =
                        $"Settlement Collector {suffix}",

                    username,

                    password,

                    email =
                        $"settlement-{suffix}@sanes.test",

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

        Assert.NotNull(
            auth);

        return TestAuthenticationHelper
            .CreateAuthenticatedClient(
                _factory,
                auth.AccessToken);
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
                    $"Settlement Investor {Guid.NewGuid():N}",

                Phone =
                    "8095551000",

                Identification =
                    $"ESI-{Guid.NewGuid():N}"
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

        Assert.NotNull(
            investor);

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
                    "Early Settlement",

                LastName =
                    $"Client {Guid.NewGuid():N}",

                Phone =
                    $"809{Random.Shared.Next(
                        1000000,
                        9999999)}",

                Address =
                    "Early Settlement Integration Test"
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

        Assert.NotNull(
            createdClient);

        return createdClient.Id;
    }

    private static async Task<LoanResponse>
        CreateLoanAsync(
            HttpClient client,
            Guid investorId,
            Guid clientId,
            DateTime startDate,
            bool lateFeeEnabled,
            decimal lateFeeAmount)
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
                    "Early settlement integration test",

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

        Assert.NotNull(
            loan);

        return loan;
    }

    private static async Task
        PayInstallmentsAsync(
            HttpClient client,
            Guid loanId,
            int count)
    {
        for (var i = 0; i < count; i++)
        {
            var response =
                await CreatePaymentAsync(
                    client,
                    loanId,
                    100m,
                    DateTime.UtcNow,
                    PaymentType.Regular);

            Assert.Equal(
                HttpStatusCode.Created,
                response.StatusCode);
        }
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

        Assert.NotNull(
            quote);

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
                        "Early settlement integration test"
                });

        response.EnsureSuccessStatusCode();

        var settlement =
            await response.Content
                .ReadFromJsonAsync<
                    EarlySettlementResponse>();

        Assert.NotNull(
            settlement);

        return settlement;
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
                    "Early settlement integration test payment"
            });
    }
}