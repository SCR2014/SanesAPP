using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Sanes.Api.IntegrationTests.Helpers;
using Sanes.Application.Clients.DTOs;
using Sanes.Application.Investors.DTOs;
using Sanes.Application.Loans.DTOs;
using Sanes.Application.Payments.DTOs;
using Sanes.Domain.Enums;

namespace Sanes.Api.IntegrationTests.Payments;

public class PaymentReversalsTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory
        _factory;

    public PaymentReversalsTests(
        CustomWebApplicationFactory factory)
    {
        _factory =
            factory;
    }

    // ============================================================
    // VALID REVERSAL
    // ============================================================

    [Fact]
    public async Task ReverseLatestPayment_RestoresLoanFinancialState()
    {
        var scenario =
            await CreateScenarioAsync();

        var originalNextPaymentDate =
            scenario.Loan.NextPaymentDate;

        var payment =
            await CreatePaymentAsync(
                scenario.AdministratorClient,
                scenario.Loan.Id,
                100m,
                PaymentType.Regular);

        var summaryAfterPayment =
            await GetSummaryAsync(
                scenario.AdministratorClient,
                scenario.Loan.Id);

        Assert.Equal(
            100m,
            summaryAfterPayment.AmountPaid);

        Assert.Equal(
            1200m,
            summaryAfterPayment.Balance);

        Assert.Equal(
            1,
            summaryAfterPayment.CompletedInstallments);

        Assert.Equal(
            originalNextPaymentDate.AddDays(7),
            summaryAfterPayment.NextPaymentDate);

        var reversalResponse =
            await scenario.AdministratorClient
                .PostAsJsonAsync(
                    $"/api/payments/{payment.Id}/reversal",
                    new PaymentReversalRequest
                    {
                        Reason =
                            "Pago registrado por duplicado"
                    });

        Assert.Equal(
            HttpStatusCode.OK,
            reversalResponse.StatusCode);

        var reversal =
            await reversalResponse.Content
                .ReadFromJsonAsync<
                    PaymentReversalResponse>();

        Assert.NotNull(
            reversal);

        Assert.NotEqual(
            Guid.Empty,
            reversal.Id);

        Assert.Equal(
            payment.Id,
            reversal.PaymentId);

        Assert.Equal(
            scenario.Loan.Id,
            reversal.LoanId);

        Assert.Equal(
            100m,
            reversal.Amount);

        Assert.Equal(
            "Pago registrado por duplicado",
            reversal.Reason);

        Assert.NotEqual(
            Guid.Empty,
            reversal.ReversedByAppUserId);

        var summaryAfterReversal =
            await GetSummaryAsync(
                scenario.AdministratorClient,
                scenario.Loan.Id);

        Assert.Equal(
            0m,
            summaryAfterReversal.AmountPaid);

        Assert.Equal(
            1300m,
            summaryAfterReversal.Balance);

        Assert.Equal(
            0,
            summaryAfterReversal.CompletedInstallments);

        Assert.Equal(
            originalNextPaymentDate,
            summaryAfterReversal.NextPaymentDate);

        Assert.Equal(
            LoanStatus.Active,
            summaryAfterReversal.Status);

        var storedPayment =
            await GetPaymentAsync(
                scenario.AdministratorClient,
                payment.Id);

        Assert.True(
            storedPayment.IsReversed);

        Assert.Equal(
            "Pago registrado por duplicado",
            storedPayment.ReversalReason);

        Assert.True(
            storedPayment.ReversedAt.HasValue);

        Assert.True(
            storedPayment.ReversedByAppUserId.HasValue);

        Assert.False(
            string.IsNullOrWhiteSpace(
                storedPayment.ReversedByName));

        /*
         * El recibo original debe seguir existiendo,
         * pero ahora marcado como reversado.
         */
        var receiptResponse =
            await scenario.AdministratorClient.GetAsync(
                $"/api/payments/{payment.Id}/receipt");

        Assert.Equal(
            HttpStatusCode.OK,
            receiptResponse.StatusCode);

        var receipt =
            await receiptResponse.Content
                .ReadFromJsonAsync<
                    PaymentReceiptResponse>();

        Assert.NotNull(
            receipt);

        Assert.True(
            receipt.IsReversed);

        Assert.Equal(
            "Pago registrado por duplicado",
            receipt.ReversalReason);

        Assert.True(
            receipt.ReversedAt.HasValue);

        Assert.True(
            receipt.ReversedByAppUserId.HasValue);

        Assert.False(
            string.IsNullOrWhiteSpace(
                receipt.ReversedByName));

        /*
         * Los datos históricos del recibo no se reescriben.
         */
        Assert.Equal(
            100m,
            receipt.AmountReceived);

        Assert.Equal(
            100m,
            receipt.LoanBalanceAmountApplied);

        Assert.Equal(
            1200m,
            receipt.ContractualBalanceAfter);
    }

    // ============================================================
    // DOUBLE REVERSAL
    // ============================================================

    [Fact]
    public async Task ReverseSamePaymentTwice_ReturnsBadRequest()
    {
        var scenario =
            await CreateScenarioAsync();

        var payment =
            await CreatePaymentAsync(
                scenario.AdministratorClient,
                scenario.Loan.Id,
                100m,
                PaymentType.Regular);

        var firstResponse =
            await ReverseAsync(
                scenario.AdministratorClient,
                payment.Id,
                "Primer reverso");

        Assert.Equal(
            HttpStatusCode.OK,
            firstResponse.StatusCode);

        var secondResponse =
            await ReverseAsync(
                scenario.AdministratorClient,
                payment.Id,
                "Segundo reverso");

        Assert.Equal(
            HttpStatusCode.BadRequest,
            secondResponse.StatusCode);

        Assert.Equal(
            "The payment has already been reversed.",
            await ReadErrorMessageAsync(
                secondResponse));
    }

    // ============================================================
    // ONLY LATEST EFFECTIVE PAYMENT
    // ============================================================

    [Fact]
    public async Task ReverseNonLatestEffectivePayment_ReturnsBadRequest()
    {
        var scenario =
            await CreateScenarioAsync();

        var firstPayment =
            await CreatePaymentAsync(
                scenario.AdministratorClient,
                scenario.Loan.Id,
                100m,
                PaymentType.Regular);

        var secondPayment =
            await CreatePaymentAsync(
                scenario.AdministratorClient,
                scenario.Loan.Id,
                100m,
                PaymentType.Regular);

        var response =
            await ReverseAsync(
                scenario.AdministratorClient,
                firstPayment.Id,
                "Intento sobre pago anterior");

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        Assert.Equal(
            "Only the latest effective payment of the loan can be reversed.",
            await ReadErrorMessageAsync(
                response));

        var firstStored =
            await GetPaymentAsync(
                scenario.AdministratorClient,
                firstPayment.Id);

        var secondStored =
            await GetPaymentAsync(
                scenario.AdministratorClient,
                secondPayment.Id);

        Assert.False(
            firstStored.IsReversed);

        Assert.False(
            secondStored.IsReversed);

        var summary =
            await GetSummaryAsync(
                scenario.AdministratorClient,
                scenario.Loan.Id);

        Assert.Equal(
            200m,
            summary.AmountPaid);

        Assert.Equal(
            1100m,
            summary.Balance);
    }

    // ============================================================
    // PAID -> ACTIVE
    // ============================================================

    [Fact]
    public async Task ReverseFullSettlement_ReactivatesLoan()
    {
        var scenario =
            await CreateScenarioAsync();

        var originalNextPaymentDate =
            scenario.Loan.NextPaymentDate;

        var payment =
            await CreatePaymentAsync(
                scenario.AdministratorClient,
                scenario.Loan.Id,
                1300m,
                PaymentType.FullSettlement);

        var paidLoan =
            await GetLoanAsync(
                scenario.AdministratorClient,
                scenario.Loan.Id);

        Assert.Equal(
            LoanStatus.Paid,
            paidLoan.Status);

        var response =
            await ReverseAsync(
                scenario.AdministratorClient,
                payment.Id,
                "Liquidación registrada incorrectamente");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var restoredLoan =
            await GetLoanAsync(
                scenario.AdministratorClient,
                scenario.Loan.Id);

        Assert.Equal(
            LoanStatus.Active,
            restoredLoan.Status);

        Assert.Equal(
            originalNextPaymentDate,
            restoredLoan.NextPaymentDate);

        var summary =
            await GetSummaryAsync(
                scenario.AdministratorClient,
                scenario.Loan.Id);

        Assert.Equal(
            0m,
            summary.AmountPaid);

        Assert.Equal(
            1300m,
            summary.Balance);

        Assert.Equal(
            0,
            summary.CompletedInstallments);

        Assert.Equal(
            LoanStatus.Active,
            summary.Status);

        Assert.Equal(
            originalNextPaymentDate,
            summary.NextPaymentDate);
    }

    // ============================================================
    // TENANT ISOLATION
    // ============================================================

    [Fact]
    public async Task ReversePayment_FromAnotherTenant_ReturnsBadRequest()
    {
        var tenant1 =
            await CreateScenarioAsync();

        var tenant2 =
            await CreateScenarioAsync();

        var payment =
            await CreatePaymentAsync(
                tenant1.AdministratorClient,
                tenant1.Loan.Id,
                100m,
                PaymentType.Regular);

        var response =
            await ReverseAsync(
                tenant2.AdministratorClient,
                payment.Id,
                "Intento desde otro tenant");

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        Assert.Equal(
            "Payment not found or does not belong to the specified tenant.",
            await ReadErrorMessageAsync(
                response));

        var originalPayment =
            await GetPaymentAsync(
                tenant1.AdministratorClient,
                payment.Id);

        Assert.False(
            originalPayment.IsReversed);

        var summary =
            await GetSummaryAsync(
                tenant1.AdministratorClient,
                tenant1.Loan.Id);

        Assert.Equal(
            100m,
            summary.AmountPaid);

        Assert.Equal(
            1200m,
            summary.Balance);
    }

    // ============================================================
    // LIFO REVERSAL
    // ============================================================

    [Fact]
    public async Task ReverseLatestThenPreviousPayment_BothCanBeReversed()
    {
        var scenario =
            await CreateScenarioAsync();

        var firstPayment =
            await CreatePaymentAsync(
                scenario.AdministratorClient,
                scenario.Loan.Id,
                100m,
                PaymentType.Regular);

        var secondPayment =
            await CreatePaymentAsync(
                scenario.AdministratorClient,
                scenario.Loan.Id,
                100m,
                PaymentType.Regular);

        var ordered =
            new[]
            {
                firstPayment,
                secondPayment
            }
            .OrderByDescending(
                x => x.CreatedAt)
            .ThenByDescending(
                x => x.Id)
            .ToList();

        var latest =
            ordered[0];

        var previous =
            ordered[1];

        var latestResponse =
            await ReverseAsync(
                scenario.AdministratorClient,
                latest.Id,
                "Reverso del último pago");

        Assert.Equal(
            HttpStatusCode.OK,
            latestResponse.StatusCode);

        var previousResponse =
            await ReverseAsync(
                scenario.AdministratorClient,
                previous.Id,
                "Reverso del pago anterior");

        Assert.Equal(
            HttpStatusCode.OK,
            previousResponse.StatusCode);

        var summary =
            await GetSummaryAsync(
                scenario.AdministratorClient,
                scenario.Loan.Id);

        Assert.Equal(
            0m,
            summary.AmountPaid);

        Assert.Equal(
            1300m,
            summary.Balance);

        Assert.Equal(
            0,
            summary.CompletedInstallments);
    }

    // ============================================================
    // REQUEST VALIDATION
    // ============================================================

    [Fact]
    public async Task ReversePayment_WithWhitespaceReason_ReturnsBadRequest()
    {
        var scenario =
            await CreateScenarioAsync();

        var payment =
            await CreatePaymentAsync(
                scenario.AdministratorClient,
                scenario.Loan.Id,
                100m,
                PaymentType.Regular);

        var response =
            await scenario.AdministratorClient
                .PostAsJsonAsync(
                    $"/api/payments/{payment.Id}/reversal",
                    new PaymentReversalRequest
                    {
                        Reason =
                            "   "
                    });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        var storedPayment =
            await GetPaymentAsync(
                scenario.AdministratorClient,
                payment.Id);

        Assert.False(
            storedPayment.IsReversed);
    }

    [Fact]
    public async Task ReversePayment_WithReasonLongerThan500_ReturnsBadRequest()
    {
        var scenario =
            await CreateScenarioAsync();

        var payment =
            await CreatePaymentAsync(
                scenario.AdministratorClient,
                scenario.Loan.Id,
                100m,
                PaymentType.Regular);

        var response =
            await scenario.AdministratorClient
                .PostAsJsonAsync(
                    $"/api/payments/{payment.Id}/reversal",
                    new PaymentReversalRequest
                    {
                        Reason =
                            new string(
                                'X',
                                501)
                    });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        var storedPayment =
            await GetPaymentAsync(
                scenario.AdministratorClient,
                payment.Id);

        Assert.False(
            storedPayment.IsReversed);
    }

    // ============================================================
    // AUTHORIZATION
    // ============================================================

    [Fact]
    public async Task ReversePayment_WithoutAuthentication_ReturnsUnauthorized()
    {
        var scenario =
            await CreateScenarioAsync();

        var payment =
            await CreatePaymentAsync(
                scenario.AdministratorClient,
                scenario.Loan.Id,
                100m,
                PaymentType.Regular);

        using var anonymousClient =
            _factory.CreateClient();

        var response =
            await ReverseAsync(
                anonymousClient,
                payment.Id,
                "Intento sin autenticación");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);

        var storedPayment =
            await GetPaymentAsync(
                scenario.AdministratorClient,
                payment.Id);

        Assert.False(
            storedPayment.IsReversed);
    }

    [Fact]
    public async Task ReversePayment_WithCollector_ReturnsForbidden()
    {
        var scenario =
            await CreateScenarioAsync();

        var payment =
            await CreatePaymentAsync(
                scenario.AdministratorClient,
                scenario.Loan.Id,
                100m,
                PaymentType.Regular);

        using var collectorClient =
            await CreateCollectorClientAsync(
                scenario.TenantId,
                scenario.AdministratorClient);

        var response =
            await ReverseAsync(
                collectorClient,
                payment.Id,
                "Intento del cobrador");

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);

        var storedPayment =
            await GetPaymentAsync(
                scenario.AdministratorClient,
                payment.Id);

        Assert.False(
            storedPayment.IsReversed);
    }

    // ============================================================
    // EARLY SETTLEMENT
    // ============================================================

    [Fact]
    public async Task ReverseEarlySettlementPayment_ReturnsBadRequest()
    {
        var scenario =
            await CreateScenarioAsync();

        for (var i = 0; i < 6; i++)
        {
            await CreatePaymentAsync(
                scenario.AdministratorClient,
                scenario.Loan.Id,
                100m,
                PaymentType.Regular);
        }

        var quoteResponse =
            await scenario.AdministratorClient
                .PostAsJsonAsync(
                    $"/api/loans/{scenario.Loan.Id}" +
                    "/early-settlement/quote",
                    new EarlySettlementQuoteRequest
                    {
                        DiscountType =
                            EarlySettlementDiscountType
                                .InstallmentWaiver,

                        DiscountValue =
                            1
                    });

        quoteResponse.EnsureSuccessStatusCode();

        var quote =
            await quoteResponse.Content
                .ReadFromJsonAsync<
                    EarlySettlementQuoteResponse>();

        Assert.NotNull(
            quote);

        Assert.True(
            quote.IsEligible);

        Assert.Equal(
            6,
            quote.CompletedInstallments);

        var settlementResponse =
            await scenario.AdministratorClient
                .PostAsJsonAsync(
                    $"/api/loans/{scenario.Loan.Id}" +
                    "/early-settlement",
                    new EarlySettlementExecuteRequest
                    {
                        DiscountType =
                            EarlySettlementDiscountType
                                .InstallmentWaiver,

                        DiscountValue =
                            1,

                        ExpectedSettlementAmount =
                            quote.SettlementAmount,

                        Reason =
                            "Liquidación anticipada de prueba"
                    });

        settlementResponse.EnsureSuccessStatusCode();

        var settlement =
            await settlementResponse.Content
                .ReadFromJsonAsync<
                    EarlySettlementResponse>();

        Assert.NotNull(
            settlement);

        Assert.True(
            settlement.PaymentId.HasValue);

        var settlementPaymentId =
            settlement.PaymentId.Value;

        var reverseResponse =
            await ReverseAsync(
                scenario.AdministratorClient,
                settlementPaymentId,
                "Intento de reversar liquidación anticipada");

        Assert.Equal(
            HttpStatusCode.BadRequest,
            reverseResponse.StatusCode);

        Assert.Equal(
            "Payments generated by an early settlement cannot be reversed from the payment reversal module.",
            await ReadErrorMessageAsync(
                reverseResponse));

        var payment =
            await GetPaymentAsync(
                scenario.AdministratorClient,
                settlementPaymentId);

        Assert.False(
            payment.IsReversed);

        var loan =
            await GetLoanAsync(
                scenario.AdministratorClient,
                scenario.Loan.Id);

        Assert.Equal(
            LoanStatus.Paid,
            loan.Status);
    }
    // ============================================================
    // HELPERS
    // ============================================================

    private async Task<PaymentReversalScenario>
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

        return new PaymentReversalScenario(
            context.TenantId,
            context.Client,
            loan);
    }

    private async Task<HttpClient>
        CreateCollectorClientAsync(
            Guid tenantId,
            HttpClient administratorClient)
    {
        var suffix =
            Guid.NewGuid()
                .ToString("N");

        var username =
            $"reversal-collector-{suffix}";

        var password =
            TestAuthenticationHelper
                .DefaultPassword;

        var createResponse =
            await administratorClient
                .PostAsJsonAsync(
                    "/api/app-users",
                    new
                    {
                        name =
                            $"Reversal Collector {suffix}",

                        username,

                        password,

                        email =
                            $"reversal-{suffix}@sanes.test",

                        phone =
                            "8095551234",

                        role =
                            (int)AppUserRole.Collector
                    });

        createResponse
            .EnsureSuccessStatusCode();

        using var loginClient =
            _factory.CreateClient();

        var token =
            await TestAuthenticationHelper
                .LoginAsync(
                    loginClient,
                    tenantId,
                    username,
                    password);

        return TestAuthenticationHelper
            .CreateAuthenticatedClient(
                _factory,
                token);
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
                        $"Reversal Investor {Guid.NewGuid():N}",

                    Phone =
                        "8095551000",

                    Identification =
                        $"REV-INV-{Guid.NewGuid():N}"
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
                        "Reversal",

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
            Guid clientId)
    {
        var startDate =
            DateTime.UtcNow.Date;

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

                    LateFeeEnabled =
                        false,

                    StartDate =
                        startDate,

                    Notes =
                        "Payment reversal integration test"
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

    private static async Task<PaymentResponse>
        CreatePaymentAsync(
            HttpClient client,
            Guid loanId,
            decimal amount,
            PaymentType paymentType)
    {
        var response =
            await client.PostAsJsonWithIdempotencyAsync("/api/payments",
                new CreatePaymentRequest
                {
                    LoanId =
                        loanId,

                    Amount =
                        amount,

                    PaymentDate =
                        DateTime.UtcNow,

                    PaymentType =
                        paymentType,

                    Notes =
                        "Payment reversal integration test"
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

    private static async Task<
        LoanFinancialSummaryResponse>
        GetSummaryAsync(
            HttpClient client,
            Guid loanId)
    {
        var response =
            await client.GetAsync(
                $"/api/loans/{loanId}/summary");

        response.EnsureSuccessStatusCode();

        var summary =
            await response.Content
                .ReadFromJsonAsync<
                    LoanFinancialSummaryResponse>();

        Assert.NotNull(
            summary);

        return summary;
    }

    private static async Task<LoanResponse>
        GetLoanAsync(
            HttpClient client,
            Guid loanId)
    {
        var response =
            await client.GetAsync(
                $"/api/loans/{loanId}");

        response.EnsureSuccessStatusCode();

        var loan =
            await response.Content
                .ReadFromJsonAsync<
                    LoanResponse>();

        Assert.NotNull(
            loan);

        return loan;
    }

    private static async Task<PaymentResponse>
        GetPaymentAsync(
            HttpClient client,
            Guid paymentId)
    {
        var response =
            await client.GetAsync(
                $"/api/payments/{paymentId}");

        response.EnsureSuccessStatusCode();

        var payment =
            await response.Content
                .ReadFromJsonAsync<
                    PaymentResponse>();

        Assert.NotNull(
            payment);

        return payment;
    }

    private static async Task<string?>
        ReadErrorMessageAsync(
            HttpResponseMessage response)
    {
        var json =
            await response.Content
                .ReadFromJsonAsync<
                    JsonElement>();

        if (json.TryGetProperty(
                "message",
                out var message))
        {
            return message.GetString();
        }

        return null;
    }

    private sealed record PaymentReversalScenario(
        Guid TenantId,
        HttpClient AdministratorClient,
        LoanResponse Loan);
}