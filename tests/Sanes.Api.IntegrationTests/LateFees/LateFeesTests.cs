using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Sanes.Api.IntegrationTests.Helpers;
using Sanes.Application.Authentication.DTOs;
using Sanes.Application.Clients.DTOs;
using Sanes.Application.Investors.DTOs;
using Sanes.Application.LateFees.DTOs;
using Sanes.Application.Loans.DTOs;
using Sanes.Application.Payments.DTOs;
using Sanes.Application.Tenants.DTOs;
using Sanes.Domain.Enums;

namespace Sanes.Api.IntegrationTests.LateFees;

public class LateFeesTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public LateFeesTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ============================================================
    // POLICY
    // ============================================================

    [Fact]
    public async Task CreateLoan_InheritsTenantLateFeePolicy()
    {
        var context =
            await CreateContextAsync();

        await UpdateTenantLateFeeDefaultsAsync(
            context,
            enabled: true,
            amount: 25m,
            graceDays: 2);

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
                DateTime.UtcNow.Date,
                useExplicitLateFeePolicy: false);

        Assert.True(
            loan.LateFeeEnabled);

        Assert.Equal(
            LateFeeCalculationType
                .FixedAmountPerInstallment,
            loan.LateFeeCalculationType);

        Assert.Equal(
            25m,
            loan.LateFeeAmount);

        Assert.Equal(
            2,
            loan.LateFeeGraceDays);
    }

    [Fact]
    public async Task CreateLoan_TenantPolicyChange_DoesNotModifyExistingLoan()
    {
        var context =
            await CreateContextAsync();

        await UpdateTenantLateFeeDefaultsAsync(
            context,
            enabled: true,
            amount: 20m,
            graceDays: 1);

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
                DateTime.UtcNow.Date,
                useExplicitLateFeePolicy: false);

        await UpdateTenantLateFeeDefaultsAsync(
            context,
            enabled: true,
            amount: 50m,
            graceDays: 5);

        var response =
            await context.Client.GetAsync(
                $"/api/loans/{loan.Id}");

        response.EnsureSuccessStatusCode();

        var storedLoan =
            await response.Content
                .ReadFromJsonAsync<LoanResponse>();

        Assert.NotNull(storedLoan);

        Assert.True(
            storedLoan.LateFeeEnabled);

        Assert.Equal(
            20m,
            storedLoan.LateFeeAmount);

        Assert.Equal(
            1,
            storedLoan.LateFeeGraceDays);
    }

    [Fact]
    public async Task CreateLoan_WithDailyPercentageLateFee_ReturnsBadRequest()
    {
        var context =
            await CreateContextAsync();

        var investorId =
            await CreateInvestorAsync(
                context.Client);

        var clientId =
            await CreateClientAsync(
                context.Client);

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
                    DateTime.UtcNow.Date,

                LateFeeEnabled =
                    true,

                LateFeeCalculationType =
                    LateFeeCalculationType
                        .DailyPercentage,

                LateFeeAmount =
                    5m,

                LateFeeGraceDays =
                    0
            };

        var response =
            await context.Client.PostAsJsonAsync(
                "/api/loans",
                request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    // ============================================================
    // ACCRUAL
    // ============================================================

    [Fact]
    public async Task GetLateFees_BeforeEffectiveDate_ReturnsNoCharges()
    {
        var setup =
            await CreateLoanScenarioAsync(
                startDate:
                    DateTime.UtcNow.Date);

        var result =
            await GetLateFeesAsync(
                setup.Context.Client,
                setup.Loan.Id);

        Assert.Equal(
            setup.Loan.Id,
            result.LoanId);

        Assert.Empty(
            result.Charges);

        Assert.Equal(
            0m,
            result.LateFeeBalance);
    }

    [Fact]
    public async Task GetLateFees_OnEffectiveDate_GeneratesFixedCharge()
    {
        /*
         * Weekly:
         *
         * StartDate = hoy - 8
         * DueDate   = hoy - 1
         * Effective = hoy
         */
        var setup =
            await CreateLoanScenarioAsync(
                startDate:
                    DateTime.UtcNow.Date
                        .AddDays(-8));

        var result =
            await GetLateFeesAsync(
                setup.Context.Client,
                setup.Loan.Id);

        var charge =
            Assert.Single(
                result.Charges);

        Assert.Equal(
            1,
            charge.InstallmentNumber);

        Assert.Equal(
            20m,
            charge.OriginalAmount);

        Assert.Equal(
            20m,
            charge.AdjustedAmount);

        Assert.Equal(
            0m,
            charge.PaidAmount);

        Assert.Equal(
            20m,
            charge.OutstandingAmount);

        Assert.Equal(
            20m,
            result.LateFeeBalance);
    }

    [Fact]
    public async Task GetLateFees_DuringGracePeriod_ReturnsNoCharges()
    {
        /*
         * StartDate = hoy - 9
         * DueDate   = hoy - 2
         *
         * GraceDays = 2
         * Effective = DueDate + 3
         *           = mañana
         */
        var setup =
            await CreateLoanScenarioAsync(
                startDate:
                    DateTime.UtcNow.Date
                        .AddDays(-9),
                graceDays: 2);

        var result =
            await GetLateFeesAsync(
                setup.Context.Client,
                setup.Loan.Id);

        Assert.Empty(
            result.Charges);

        Assert.Equal(
            0m,
            result.LateFeeBalance);
    }

    [Fact]
    public async Task GetLateFees_FullInstallmentPaidBeforeEffectiveDate_DoesNotGenerateCharge()
    {
        var today =
            DateTime.UtcNow.Date;

        var setup =
            await CreateLoanScenarioAsync(
                startDate:
                    today.AddDays(-8));

        /*
         * DueDate = ayer.
         * EffectiveDate = hoy.
         *
         * El pago ocurrió ayer, antes de iniciar
         * la mora.
         */
        var paymentResponse =
            await CreatePaymentAsync(
                setup.Context.Client,
                setup.Loan.Id,
                amount: 100m,
                paymentDate:
                    today.AddDays(-1),
                paymentType:
                    PaymentType.Regular);

        Assert.Equal(
            HttpStatusCode.Created,
            paymentResponse.StatusCode);

        var result =
            await GetLateFeesAsync(
                setup.Context.Client,
                setup.Loan.Id);

        Assert.Empty(
            result.Charges);

        Assert.Equal(
            0m,
            result.LateFeeBalance);
    }

    [Fact]
    public async Task GetLateFees_PartialInstallmentPaidBeforeEffectiveDate_GeneratesFullCharge()
    {
        var today =
            DateTime.UtcNow.Date;

        var setup =
            await CreateLoanScenarioAsync(
                startDate:
                    today.AddDays(-8));

        var paymentResponse =
            await CreatePaymentAsync(
                setup.Context.Client,
                setup.Loan.Id,
                amount: 50m,
                paymentDate:
                    today.AddDays(-1),
                paymentType:
                    PaymentType.Partial);

        Assert.Equal(
            HttpStatusCode.Created,
            paymentResponse.StatusCode);

        var result =
            await GetLateFeesAsync(
                setup.Context.Client,
                setup.Loan.Id);

        var charge =
            Assert.Single(
                result.Charges);

        /*
         * Aunque solamente faltaban RD$50 de la cuota,
         * el MVP cobra la mora fija completa.
         */
        Assert.Equal(
            20m,
            charge.OriginalAmount);

        Assert.Equal(
            20m,
            charge.OutstandingAmount);

        Assert.Equal(
            20m,
            result.LateFeeBalance);
    }

    [Fact]
    public async Task GetLateFees_MultipleOverdueInstallments_GeneratesOneChargePerInstallment()
    {
        /*
         * Start = hoy - 22.
         *
         * Weekly:
         * #1 due -15 / effective -14
         * #2 due  -8 / effective  -7
         * #3 due  -1 / effective   0
         *
         * Las tres ya generan mora.
         */
        var setup =
            await CreateLoanScenarioAsync(
                startDate:
                    DateTime.UtcNow.Date
                        .AddDays(-22));

        var result =
            await GetLateFeesAsync(
                setup.Context.Client,
                setup.Loan.Id);

        Assert.Equal(
            3,
            result.Charges.Count);

        Assert.Equal(
            new[]
            {
                1,
                2,
                3
            },
            result.Charges
                .Select(x =>
                    x.InstallmentNumber)
                .ToArray());

        Assert.All(
            result.Charges,
            charge =>
                Assert.Equal(
                    20m,
                    charge.OriginalAmount));

        Assert.Equal(
            60m,
            result.LateFeeBalance);
    }

    [Fact]
    public async Task GetLateFees_RepeatedAccrual_DoesNotDuplicateCharges()
    {
        var setup =
            await CreateLoanScenarioAsync(
                startDate:
                    DateTime.UtcNow.Date
                        .AddDays(-8));

        var first =
            await GetLateFeesAsync(
                setup.Context.Client,
                setup.Loan.Id);

        var second =
            await GetLateFeesAsync(
                setup.Context.Client,
                setup.Loan.Id);

        var firstCharge =
            Assert.Single(
                first.Charges);

        var secondCharge =
            Assert.Single(
                second.Charges);

        Assert.Equal(
            firstCharge.Id,
            secondCharge.Id);

        Assert.Equal(
            20m,
            second.LateFeeBalance);
    }

    // ============================================================
    // PAYMENT DISTRIBUTION
    // ============================================================

    [Fact]
    public async Task Payment_WhenLateFeeExists_AppliesMoneyToLateFeeFirst()
    {
        var setup =
            await CreateLoanScenarioAsync(
                startDate:
                    DateTime.UtcNow.Date
                        .AddDays(-8));

        var originalNextPaymentDate =
            setup.Loan.NextPaymentDate;

        /*
         * Materializamos primero la mora.
         */
        var before =
            await GetLateFeesAsync(
                setup.Context.Client,
                setup.Loan.Id);

        Assert.Equal(
            20m,
            before.LateFeeBalance);

        /*
         * RD$100 recibidos:
         *
         * 20 -> mora
         * 80 -> préstamo
         *
         * Como todavía no se completaron RD$100
         * contractuales, NextPaymentDate no debe avanzar.
         */
        var response =
            await CreatePaymentAsync(
                setup.Context.Client,
                setup.Loan.Id,
                amount: 100m,
                paymentDate:
                    DateTime.UtcNow,
                paymentType:
                    PaymentType.Partial);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var lateFeesAfter =
            await GetLateFeesAsync(
                setup.Context.Client,
                setup.Loan.Id);

        Assert.Equal(
            0m,
            lateFeesAfter.LateFeeBalance);

        var loanResponse =
            await setup.Context.Client.GetAsync(
                $"/api/loans/{setup.Loan.Id}");

        loanResponse.EnsureSuccessStatusCode();

        var loanAfter =
            await loanResponse.Content
                .ReadFromJsonAsync<LoanResponse>();

        Assert.NotNull(loanAfter);

        Assert.Equal(
            originalNextPaymentDate,
            loanAfter.NextPaymentDate);

        /*
         * Los RD$20 siguientes completan los RD$100
         * contractuales de la cuota.
         */
        var secondPayment =
            await CreatePaymentAsync(
                setup.Context.Client,
                setup.Loan.Id,
                amount: 20m,
                paymentDate:
                    DateTime.UtcNow,
                paymentType:
                    PaymentType.Partial);

        Assert.Equal(
            HttpStatusCode.Created,
            secondPayment.StatusCode);

        var finalLoanResponse =
            await setup.Context.Client.GetAsync(
                $"/api/loans/{setup.Loan.Id}");

        finalLoanResponse.EnsureSuccessStatusCode();

        var finalLoan =
            await finalLoanResponse.Content
                .ReadFromJsonAsync<LoanResponse>();

        Assert.NotNull(finalLoan);

        Assert.Equal(
            originalNextPaymentDate.AddDays(7),
            finalLoan.NextPaymentDate);
    }

    [Fact]
    public async Task Payment_CannotExceedContractualBalancePlusLateFees()
    {
        var setup =
            await CreateLoanScenarioAsync(
                startDate:
                    DateTime.UtcNow.Date
                        .AddDays(-8));

        var lateFees =
            await GetLateFeesAsync(
                setup.Context.Client,
                setup.Loan.Id);

        Assert.Equal(
            20m,
            lateFees.LateFeeBalance);

        /*
         * Contractual = 1300
         * Mora        =   20
         * Total       = 1320
         */
        var response =
            await CreatePaymentAsync(
                setup.Context.Client,
                setup.Loan.Id,
                amount: 1320.01m,
                paymentDate:
                    DateTime.UtcNow,
                paymentType:
                    PaymentType.FullSettlement);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    // ============================================================
    // ADMINISTRATION
    // ============================================================

    [Fact]
    public async Task AddAdjustment_Waiver_ReducesOutstandingLateFeeAndPreservesHistory()
    {
        var setup =
            await CreateLoanScenarioAsync(
                startDate:
                    DateTime.UtcNow.Date
                        .AddDays(-8));

        var lateFees =
            await GetLateFeesAsync(
                setup.Context.Client,
                setup.Loan.Id);

        var charge =
            Assert.Single(
                lateFees.Charges);

        var request =
            new LateFeeAdjustmentRequest
            {
                AdjustmentType =
                    LateFeeAdjustmentType.Waiver,

                Amount =
                    5m,

                Reason =
                    "Prueba de condonación parcial"
            };

        var response =
            await setup.Context.Client.PostAsJsonAsync(
                $"/api/late-fees/charges/{charge.Id}/adjustments",
                request);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var adjusted =
            await response.Content
                .ReadFromJsonAsync<
                    LateFeeChargeResponse>();

        Assert.NotNull(adjusted);

        Assert.Equal(
            20m,
            adjusted.OriginalAmount);

        Assert.Equal(
            5m,
            adjusted.WaivedAmount);

        Assert.Equal(
            15m,
            adjusted.AdjustedAmount);

        Assert.Equal(
            15m,
            adjusted.OutstandingAmount);

        var history =
            await GetLateFeesAsync(
                setup.Context.Client,
                setup.Loan.Id);

        var historicalCharge =
            Assert.Single(
                history.Charges);

        Assert.Equal(
            20m,
            historicalCharge.OriginalAmount);

        Assert.Equal(
            5m,
            historicalCharge.WaivedAmount);

        var adjustment =
            Assert.Single(
                historicalCharge.Adjustments);

        Assert.Equal(
            LateFeeAdjustmentType.Waiver,
            adjustment.AdjustmentType);

        Assert.Equal(
            "Prueba de condonación parcial",
            adjustment.Reason);
    }

    [Fact]
    public async Task AddAdjustment_DecreaseGreaterThanOutstanding_ReturnsBadRequest()
    {
        var setup =
            await CreateLoanScenarioAsync(
                startDate:
                    DateTime.UtcNow.Date
                        .AddDays(-8));

        var lateFees =
            await GetLateFeesAsync(
                setup.Context.Client,
                setup.Loan.Id);

        var charge =
            Assert.Single(
                lateFees.Charges);

        var response =
            await setup.Context.Client.PostAsJsonAsync(
                $"/api/late-fees/charges/{charge.Id}/adjustments",
                new LateFeeAdjustmentRequest
                {
                    AdjustmentType =
                        LateFeeAdjustmentType.Decrease,

                    Amount =
                        21m,

                    Reason =
                        "Monto superior al saldo"
                });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task LateFeeEndpoint_WithCollector_ReturnsForbidden()
    {
        var setup =
            await CreateLoanScenarioAsync(
                startDate:
                    DateTime.UtcNow.Date
                        .AddDays(-8));

        var collector =
            await CreateCollectorAsync(
                setup.Context);

        var collectorClient =
            await LoginCollectorAsync(
                setup.Context.TenantId,
                collector);

        var response =
            await collectorClient.GetAsync(
                $"/api/late-fees/loans/{setup.Loan.Id}");

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task LateFeeEndpoint_CannotAccessLoanFromAnotherTenant()
    {
        var tenant1 =
            await CreateContextAsync();

        var tenant2Scenario =
            await CreateLoanScenarioAsync(
                startDate:
                    DateTime.UtcNow.Date
                        .AddDays(-8));

        var response =
            await tenant1.Client.GetAsync(
                $"/api/late-fees/loans/{tenant2Scenario.Loan.Id}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    // ============================================================
    // SCENARIOS
    // ============================================================

    private async Task<LoanScenario>
        CreateLoanScenarioAsync(
            DateTime startDate,
            decimal lateFeeAmount = 20m,
            int graceDays = 0)
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
                lateFeeEnabled: true,
                lateFeeAmount:
                    lateFeeAmount,
                graceDays:
                    graceDays);

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
            $"latefee-collector-{suffix}";

        var password =
            TestAuthenticationHelper
                .DefaultPassword;

        var response =
            await context.Client.PostAsJsonAsync(
                "/api/app-users",
                new
                {
                    name =
                        $"Late Fee Collector {suffix}",

                    username,

                    password,

                    email =
                        $"latefee-{suffix}@sanes.test",

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

        Assert.NotNull(auth);

        return TestAuthenticationHelper
            .CreateAuthenticatedClient(
                _factory,
                auth.AccessToken);
    }

    // ============================================================
    // TENANT
    // ============================================================

    private static async Task
        UpdateTenantLateFeeDefaultsAsync(
            TestTenantContext context,
            bool enabled,
            decimal amount,
            int graceDays)
    {
        var request =
            new UpdateTenantRequest
            {
                Name =
                    $"Tenant Late Fee {Guid.NewGuid():N}",

                LegalName =
                    "Tenant Late Fee Test SRL",

                Phone =
                    "8095551234",

                Email =
                    $"latefee-{Guid.NewGuid():N}@sanes.test",

                CurrencyCode =
                    "DOP",

                CurrencySymbol =
                    "RD$",

                DefaultLateFeeEnabled =
                    enabled,

                DefaultLateFeeCalculationType =
                    LateFeeCalculationType
                        .FixedAmountPerInstallment,

                DefaultLateFeeAmount =
                    amount,

                DefaultLateFeeGraceDays =
                    graceDays
            };

        var response =
            await context.Client.PutAsJsonAsync(
                "/api/tenants/me",
                request);

        response.EnsureSuccessStatusCode();
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
                    $"Late Fee Investor {Guid.NewGuid():N}",

                Phone =
                    "8095551000",

                Identification =
                    $"LFI-{Guid.NewGuid():N}"
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
                    "Late Fee",

                LastName =
                    $"Client {Guid.NewGuid():N}",

                Phone =
                    $"809{Random.Shared.Next(
                        1000000,
                        9999999)}",

                Address =
                    "Late Fee Integration Test"
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
            bool lateFeeEnabled = true,
            decimal lateFeeAmount = 20m,
            int graceDays = 0,
            bool useExplicitLateFeePolicy = true)
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
                    "Late fee integration test"
            };

        if (useExplicitLateFeePolicy)
        {
            request.LateFeeEnabled =
                lateFeeEnabled;

            request.LateFeeCalculationType =
                LateFeeCalculationType
                    .FixedAmountPerInstallment;

            request.LateFeeAmount =
                lateFeeAmount;

            request.LateFeeGraceDays =
                graceDays;
        }

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

        Assert.NotNull(result);

        return result;
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
                    "Late fee integration test payment"
            });
    }
}