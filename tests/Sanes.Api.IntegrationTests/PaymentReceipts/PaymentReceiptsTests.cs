using System.Net;
using System.Net.Http.Json;
using Sanes.Api.IntegrationTests.Helpers;
using Sanes.Application.Authentication.DTOs;
using Sanes.Application.Clients.DTOs;
using Sanes.Application.Investors.DTOs;
using Sanes.Application.Loans.DTOs;
using Sanes.Application.Payments.DTOs;
using Microsoft.Extensions.DependencyInjection;
using Sanes.Application.Common.Persistence;
using Sanes.Application.Payments.Repositories;
using Sanes.Domain.Enums;

namespace Sanes.Api.IntegrationTests.PaymentReceipts;

public class PaymentReceiptsTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public PaymentReceiptsTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ============================================================
    // CREATION
    // ============================================================

    [Fact]
    public async Task CreatePayment_AutomaticallyCreatesReceipt()
    {
        var scenario =
            await CreateScenarioAsync();

        var payment =
            await CreatePaymentAsync(
                scenario.Context.Client,
                scenario.Loan.Id,
                100m);

        var response =
            await scenario.Context.Client.GetAsync(
                $"/api/payments/{payment.Id}/receipt");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var receipt =
            await response.Content
                .ReadFromJsonAsync<PaymentReceiptResponse>();

        Assert.NotNull(receipt);

        Assert.NotEqual(
            Guid.Empty,
            receipt.Id);

        Assert.Equal(
            payment.Id,
            receipt.PaymentId);

        Assert.Equal(
            scenario.Loan.Id,
            receipt.LoanId);

        Assert.Equal(
            scenario.Client.Id,
            receipt.ClientId);

        Assert.Equal(
            $"{scenario.Client.FirstName} {scenario.Client.LastName}",
            receipt.ClientName);

        Assert.Equal(
            "DOP",
            receipt.CurrencyCode);

        Assert.Equal(
            "RD$",
            receipt.CurrencySymbol);

        Assert.Equal(
            100m,
            receipt.AmountReceived);

        Assert.Equal(
            0m,
            receipt.LateFeeAmountApplied);

        Assert.Equal(
            100m,
            receipt.LoanBalanceAmountApplied);

        /*
         * Loan de prueba:
         * 13 x 100 = 1300.
         * Después de pagar 100 quedan 1200.
         */
        Assert.Equal(
            1200m,
            receipt.ContractualBalanceAfter);

        Assert.Equal(
            0m,
            receipt.LateFeeBalanceAfter);

        Assert.Equal(
            1200m,
            receipt.TotalOutstandingAfter);

        Assert.Equal(
            PaymentType.Regular,
            receipt.PaymentType);

        Assert.Null(
            receipt.CollectedByAppUserId);

        Assert.Null(
            receipt.CollectedByName);
    }

    // ============================================================
    // LOOKUPS
    // ============================================================

    [Fact]
    public async Task GetByPayment_ReturnsExistingReceipt()
    {
        var scenario =
            await CreateScenarioAsync();

        var payment =
            await CreatePaymentAsync(
                scenario.Context.Client,
                scenario.Loan.Id,
                100m);

        var response =
            await scenario.Context.Client.GetAsync(
                $"/api/payments/{payment.Id}/receipt");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var receipt =
            await response.Content
                .ReadFromJsonAsync<PaymentReceiptResponse>();

        Assert.NotNull(receipt);

        Assert.Equal(
            payment.Id,
            receipt.PaymentId);
    }

    [Fact]
    public async Task GetById_ReturnsSameReceipt()
    {
        var scenario =
            await CreateScenarioAsync();

        var payment =
            await CreatePaymentAsync(
                scenario.Context.Client,
                scenario.Loan.Id,
                100m);

        var receipt =
            await GetReceiptByPaymentAsync(
                scenario.Context.Client,
                payment.Id);

        var response =
            await scenario.Context.Client.GetAsync(
                $"/api/payment-receipts/{receipt.Id}");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<PaymentReceiptResponse>();

        Assert.NotNull(result);

        Assert.Equal(
            receipt.Id,
            result.Id);

        Assert.Equal(
            receipt.ReceiptNumber,
            result.ReceiptNumber);
    }

    [Fact]
    public async Task GetByNumber_IsCaseInsensitive()
    {
        var scenario =
            await CreateScenarioAsync();

        var payment =
            await CreatePaymentAsync(
                scenario.Context.Client,
                scenario.Loan.Id,
                100m);

        var receipt =
            await GetReceiptByPaymentAsync(
                scenario.Context.Client,
                payment.Id);

        var response =
            await scenario.Context.Client.GetAsync(
                "/api/payment-receipts/by-number/" +
                receipt.ReceiptNumber.ToLowerInvariant());

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<PaymentReceiptResponse>();

        Assert.NotNull(result);

        Assert.Equal(
            receipt.Id,
            result.Id);
    }

    // ============================================================
    // SEQUENCE
    // ============================================================

    [Fact]
    public async Task TwoPayments_UseSequentialReceiptNumbers()
    {
        var scenario =
            await CreateScenarioAsync();

        var firstPayment =
            await CreatePaymentAsync(
                scenario.Context.Client,
                scenario.Loan.Id,
                100m);

        var secondPayment =
            await CreatePaymentAsync(
                scenario.Context.Client,
                scenario.Loan.Id,
                100m);

        var firstReceipt =
            await GetReceiptByPaymentAsync(
                scenario.Context.Client,
                firstPayment.Id);

        var secondReceipt =
            await GetReceiptByPaymentAsync(
                scenario.Context.Client,
                secondPayment.Id);

        Assert.Equal(
            1,
            firstReceipt.SequenceNumber);

        Assert.Equal(
            2,
            secondReceipt.SequenceNumber);

        Assert.Equal(
            firstReceipt.ReceiptYear,
            secondReceipt.ReceiptYear);

        Assert.Equal(
            $"REC-{firstReceipt.ReceiptYear}-000001",
            firstReceipt.ReceiptNumber);

        Assert.Equal(
            $"REC-{secondReceipt.ReceiptYear}-000002",
            secondReceipt.ReceiptNumber);
    }

    [Fact]
    public async Task DifferentTenants_HaveIndependentSequences()
    {
        var firstScenario =
            await CreateScenarioAsync();

        var secondScenario =
            await CreateScenarioAsync();

        var firstPayment =
            await CreatePaymentAsync(
                firstScenario.Context.Client,
                firstScenario.Loan.Id,
                100m);

        var secondPayment =
            await CreatePaymentAsync(
                secondScenario.Context.Client,
                secondScenario.Loan.Id,
                100m);

        var firstReceipt =
            await GetReceiptByPaymentAsync(
                firstScenario.Context.Client,
                firstPayment.Id);

        var secondReceipt =
            await GetReceiptByPaymentAsync(
                secondScenario.Context.Client,
                secondPayment.Id);

        Assert.Equal(
            1,
            firstReceipt.SequenceNumber);

        Assert.Equal(
            1,
            secondReceipt.SequenceNumber);
    }

    [Fact]
    public async Task ReceiptSequence_WhenTransactionRollsBack_DoesNotConsumeNumber()
    {
        var context =
            await TestAuthenticationHelper
                .CreateAdministratorContextAsync(
                    _factory);

        var year =
            DateTime.UtcNow.Year;

        /*
        * Primera transacción:
        *
        * obtenemos #1 y provocamos intencionalmente
        * un fallo antes del commit.
        */
        await using (
            var rollbackScope =
                _factory.Services
                    .CreateAsyncScope())
        {
            var transactionRunner =
                rollbackScope.ServiceProvider
                    .GetRequiredService<
                        ITransactionRunner>();

            var receiptRepository =
                rollbackScope.ServiceProvider
                    .GetRequiredService<
                        IPaymentReceiptRepository>();

            await Assert.ThrowsAsync<
                InvalidOperationException>(
                async () =>
                {
                    await transactionRunner.ExecuteAsync(
                        async cancellationToken =>
                        {
                            var sequenceNumber =
                                await receiptRepository
                                    .GetNextSequenceNumberAsync(
                                        context.TenantId,
                                        year,
                                        cancellationToken);

                            Assert.Equal(
                                1,
                                sequenceNumber);

                            if (sequenceNumber == 1)
                            {
                                throw new InvalidOperationException(
                                    "Intentional transaction rollback.");
                            }

                            return sequenceNumber;
                        });
                });
        }

        /*
        * Segunda transacción.
        *
        * Si el rollback funcionó correctamente,
        * el siguiente número sigue siendo #1.
        */
        await using var commitScope =
            _factory.Services
                .CreateAsyncScope();

        var committedTransactionRunner =
            commitScope.ServiceProvider
                .GetRequiredService<
                    ITransactionRunner>();

        var committedReceiptRepository =
            commitScope.ServiceProvider
                .GetRequiredService<
                    IPaymentReceiptRepository>();

        var nextNumber =
            await committedTransactionRunner.ExecuteAsync(
                cancellationToken =>
                    committedReceiptRepository
                        .GetNextSequenceNumberAsync(
                            context.TenantId,
                            year,
                            cancellationToken));

        Assert.Equal(
            1,
            nextNumber);
    }

    // ============================================================
    // TENANT ISOLATION
    // ============================================================

    [Fact]
    public async Task CrossTenant_GetById_ReturnsNotFound()
    {
        var tenant1 =
            await CreateScenarioAsync();

        var tenant2 =
            await CreateScenarioAsync();

        var payment =
            await CreatePaymentAsync(
                tenant2.Context.Client,
                tenant2.Loan.Id,
                100m);

        var receipt =
            await GetReceiptByPaymentAsync(
                tenant2.Context.Client,
                payment.Id);

        var response =
            await tenant1.Context.Client.GetAsync(
                $"/api/payment-receipts/{receipt.Id}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task CrossTenant_GetByNumber_ReturnsNotFound()
    {
        var tenant1 =
            await CreateScenarioAsync();

        var tenant2 =
            await CreateScenarioAsync();

        var payment =
            await CreatePaymentAsync(
                tenant2.Context.Client,
                tenant2.Loan.Id,
                100m);

        var receipt =
            await GetReceiptByPaymentAsync(
                tenant2.Context.Client,
                payment.Id);

        var response =
            await tenant1.Context.Client.GetAsync(
                "/api/payment-receipts/by-number/" +
                receipt.ReceiptNumber);

        /*
         * El Tenant 1 puede tener el mismo número visible
         * en su propia secuencia, pero no debe poder resolver
         * el registro perteneciente al Tenant 2.
         */
        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    // ============================================================
    // AUTHENTICATION / AUTHORIZATION
    // ============================================================

    [Fact]
    public async Task ReceiptEndpoint_WithoutToken_ReturnsUnauthorized()
    {
        using var client =
            _factory.CreateClient();

        var response =
            await client.GetAsync(
                $"/api/payment-receipts/{Guid.NewGuid()}");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task Collector_CanReadReceipt()
    {
        var scenario =
            await CreateScenarioAsync();

        var payment =
            await CreatePaymentAsync(
                scenario.Context.Client,
                scenario.Loan.Id,
                100m);

        var receipt =
            await GetReceiptByPaymentAsync(
                scenario.Context.Client,
                payment.Id);

        var collector =
            await CreateCollectorAsync(
                scenario.Context);

        using var loginClient =
            _factory.CreateClient();

        var token =
            await TestAuthenticationHelper.LoginAsync(
                loginClient,
                scenario.Context.TenantId,
                collector.Username,
                collector.Password);

        using var collectorClient =
            TestAuthenticationHelper
                .CreateAuthenticatedClient(
                    _factory,
                    token);

        var response =
            await collectorClient.GetAsync(
                $"/api/payment-receipts/{receipt.Id}");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);
    }

    // ============================================================
    // SCENARIO
    // ============================================================

    private async Task<ReceiptScenario>
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
                client.Id);

        return new ReceiptScenario(
            context,
            investor,
            client,
            loan);
    }

    // ============================================================
    // PAYMENT
    // ============================================================

    private static async Task<PaymentResponse>
        CreatePaymentAsync(
            HttpClient client,
            Guid loanId,
            decimal amount)
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
                        DateTime.UtcNow,

                    PaymentType =
                        PaymentType.Regular,

                    Notes =
                        "Payment receipt integration test"
                });

        response.EnsureSuccessStatusCode();

        var payment =
            await response.Content
                .ReadFromJsonAsync<PaymentResponse>();

        Assert.NotNull(
            payment);

        return payment;
    }

    private static async Task<PaymentReceiptResponse>
        GetReceiptByPaymentAsync(
            HttpClient client,
            Guid paymentId)
    {
        var response =
            await client.GetAsync(
                $"/api/payments/{paymentId}/receipt");

        response.EnsureSuccessStatusCode();

        var receipt =
            await response.Content
                .ReadFromJsonAsync<PaymentReceiptResponse>();

        Assert.NotNull(
            receipt);

        return receipt;
    }

    // ============================================================
    // INVESTOR / CLIENT / LOAN
    // ============================================================

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
                        $"Receipt Investor {Guid.NewGuid():N}",

                    Phone =
                        "8095551000",

                    Identification =
                        $"REC-INV-{Guid.NewGuid():N}"
                });

        response.EnsureSuccessStatusCode();

        var investor =
            await response.Content
                .ReadFromJsonAsync<InvestorResponse>();

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
                        "Receipt",

                    LastName =
                        $"Client-{Guid.NewGuid():N}",

                    Phone =
                        $"809{Random.Shared.Next(
                            1000000,
                            9999999)}",

                    Address =
                        "Santiago"
                });

        response.EnsureSuccessStatusCode();

        var createdClient =
            await response.Content
                .ReadFromJsonAsync<ClientResponse>();

        Assert.NotNull(
            createdClient);

        return createdClient;
    }

    private static async Task<LoanResponse>
        CreateLoanAsync(
            HttpClient client,
            Guid investorId,
            Guid clientId)
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

                    /*
                     * Fecha actual para evitar que este
                     * escenario genere mora.
                     */
                    StartDate =
                        DateTime.UtcNow.Date,

                    Notes =
                        "Loan for payment receipt test"
                });

        response.EnsureSuccessStatusCode();

        var loan =
            await response.Content
                .ReadFromJsonAsync<LoanResponse>();

        Assert.NotNull(
            loan);

        return loan;
    }

    // ============================================================
    // COLLECTOR
    // ============================================================

    private static async Task<CollectorCredentials>
        CreateCollectorAsync(
            TestTenantContext context)
    {
        var suffix =
            Guid.NewGuid().ToString("N");

        var username =
            $"receipt-collector-{suffix}";

        var password =
            TestAuthenticationHelper.DefaultPassword;

        var response =
            await context.Client.PostAsJsonAsync(
                "/api/app-users",
                new
                {
                    name =
                        $"Receipt Collector {suffix}",

                    username,

                    password,

                    email =
                        $"receipt-{suffix}@sanes.test",

                    phone =
                        "8095551234",

                    role =
                        (int)AppUserRole.Collector
                });

        response.EnsureSuccessStatusCode();

        return new CollectorCredentials(
            username,
            password);
    }

    private sealed record ReceiptScenario(
        TestTenantContext Context,
        InvestorResponse Investor,
        ClientResponse Client,
        LoanResponse Loan);

    private sealed record CollectorCredentials(
        string Username,
        string Password);
}