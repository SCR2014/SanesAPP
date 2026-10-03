using System.Net;
using System.Net.Http.Json;
using Sanes.Api.IntegrationTests.Helpers;
using Sanes.Application.Clients.DTOs;
using Sanes.Application.Investors.DTOs;
using Sanes.Application.Loans.DTOs;
using Sanes.Application.Payments.DTOs;
using Sanes.Application.LateFees.DTOs;
using Sanes.Domain.Enums;

namespace Sanes.Api.IntegrationTests.Payments;

public class PaymentIdempotencyTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public PaymentIdempotencyTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ============================================================
    // SAME KEY + SAME REQUEST
    // ============================================================

    [Fact]
    public async Task
        CreatePayment_SameKeyAndSameRequest_ReturnsSamePaymentWithoutDuplicatingEffects()
    {
        var scenario =
            await CreateScenarioAsync();

        var idempotencyKey =
            Guid.NewGuid();

        var request =
            CreatePaymentRequest(
                scenario.Loan.Id,
                100m);

        var firstResponse =
            await scenario.Client
                .PostAsJsonWithIdempotencyAsync(
                    "/api/payments",
                    request,
                    idempotencyKey);

        var secondResponse =
            await scenario.Client
                .PostAsJsonWithIdempotencyAsync(
                    "/api/payments",
                    request,
                    idempotencyKey);

        Assert.Equal(
            HttpStatusCode.Created,
            firstResponse.StatusCode);

        Assert.Equal(
            HttpStatusCode.Created,
            secondResponse.StatusCode);

        var firstPayment =
            await firstResponse.Content
                .ReadFromJsonAsync<PaymentResponse>();

        var secondPayment =
            await secondResponse.Content
                .ReadFromJsonAsync<PaymentResponse>();

        Assert.NotNull(
            firstPayment);

        Assert.NotNull(
            secondPayment);

        Assert.Equal(
            firstPayment.Id,
            secondPayment.Id);

        var payments =
            await GetPaymentsAsync(
                scenario.Client,
                scenario.Loan.Id);

        var storedPayment =
            Assert.Single(
                payments);

        Assert.Equal(
            firstPayment.Id,
            storedPayment.Id);

        Assert.Equal(
            100m,
            storedPayment.Amount);

        var receiptResponse =
            await scenario.Client.GetAsync(
                $"/api/payments/{firstPayment.Id}/receipt");

        Assert.Equal(
            HttpStatusCode.OK,
            receiptResponse.StatusCode);

        var receipt =
            await receiptResponse.Content
                .ReadFromJsonAsync<
                    PaymentReceiptResponse>();

        Assert.NotNull(
            receipt);

        Assert.Equal(
            firstPayment.Id,
            receipt.PaymentId);

        Assert.Equal(
            100m,
            receipt.AmountReceived);

        Assert.Equal(
            100m,
            receipt.LoanBalanceAmountApplied);
    }

    // ============================================================
    // SAME KEY + DIFFERENT REQUEST
    // ============================================================

    [Fact]
    public async Task
        CreatePayment_SameKeyAndDifferentRequest_ReturnsConflictAndDoesNotCreateSecondPayment()
    {
        var scenario =
            await CreateScenarioAsync();

        var idempotencyKey =
            Guid.NewGuid();

        var paymentDate =
            DateTime.UtcNow;

        var firstRequest =
            new CreatePaymentRequest
            {
                LoanId =
                    scenario.Loan.Id,

                Amount =
                    100m,

                PaymentDate =
                    paymentDate,

                PaymentType =
                    PaymentType.Regular,

                Notes =
                    "Original idempotency request"
            };

        var conflictingRequest =
            new CreatePaymentRequest
            {
                LoanId =
                    scenario.Loan.Id,

                Amount =
                    50m,

                PaymentDate =
                    paymentDate,

                PaymentType =
                    PaymentType.Partial,

                Notes =
                    "Different idempotency request"
            };

        var firstResponse =
            await scenario.Client
                .PostAsJsonWithIdempotencyAsync(
                    "/api/payments",
                    firstRequest,
                    idempotencyKey);

        Assert.Equal(
            HttpStatusCode.Created,
            firstResponse.StatusCode);

        var firstPayment =
            await firstResponse.Content
                .ReadFromJsonAsync<PaymentResponse>();

        Assert.NotNull(
            firstPayment);

        var conflictResponse =
            await scenario.Client
                .PostAsJsonWithIdempotencyAsync(
                    "/api/payments",
                    conflictingRequest,
                    idempotencyKey);

        Assert.Equal(
            HttpStatusCode.Conflict,
            conflictResponse.StatusCode);

        var payments =
            await GetPaymentsAsync(
                scenario.Client,
                scenario.Loan.Id);

        var storedPayment =
            Assert.Single(
                payments);

        Assert.Equal(
            firstPayment.Id,
            storedPayment.Id);

        Assert.Equal(
            100m,
            storedPayment.Amount);
    }

    // ============================================================
    // CONCURRENCY
    // ============================================================

    [Fact]
    public async Task
        CreatePayment_SameKeyConcurrently_CreatesSinglePayment()
    {
        var scenario =
            await CreateScenarioAsync();

        var idempotencyKey =
            Guid.NewGuid();

        var request =
            CreatePaymentRequest(
                scenario.Loan.Id,
                100m);

        var firstTask =
            scenario.Client
                .PostAsJsonWithIdempotencyAsync(
                    "/api/payments",
                    request,
                    idempotencyKey);

        var secondTask =
            scenario.Client
                .PostAsJsonWithIdempotencyAsync(
                    "/api/payments",
                    request,
                    idempotencyKey);

        await Task.WhenAll(
            firstTask,
            secondTask);

        using var firstResponse =
            await firstTask;

        using var secondResponse =
            await secondTask;

        Assert.Equal(
            HttpStatusCode.Created,
            firstResponse.StatusCode);

        Assert.Equal(
            HttpStatusCode.Created,
            secondResponse.StatusCode);

        var firstPayment =
            await firstResponse.Content
                .ReadFromJsonAsync<PaymentResponse>();

        var secondPayment =
            await secondResponse.Content
                .ReadFromJsonAsync<PaymentResponse>();

        Assert.NotNull(
            firstPayment);

        Assert.NotNull(
            secondPayment);

        Assert.Equal(
            firstPayment.Id,
            secondPayment.Id);

        var payments =
            await GetPaymentsAsync(
                scenario.Client,
                scenario.Loan.Id);

        var storedPayment =
            Assert.Single(
                payments);

        Assert.Equal(
            firstPayment.Id,
            storedPayment.Id);

        Assert.Equal(
            100m,
            storedPayment.Amount);
    }

    [Fact]
    public async Task
        CreatePayment_DifferentKeysConcurrently_BothValidPaymentsUseSerializedLoanState()
    {
        var scenario =
            await CreateScenarioAsync();

        var paymentDate =
            DateTime.UtcNow;

        var firstRequest =
            new CreatePaymentRequest
            {
                LoanId =
                    scenario.Loan.Id,

                Amount =
                    100m,

                PaymentDate =
                    paymentDate,

                PaymentType =
                    PaymentType.Regular,

                Notes =
                    "Concurrent payment A"
            };

        var secondRequest =
            new CreatePaymentRequest
            {
                LoanId =
                    scenario.Loan.Id,

                Amount =
                    100m,

                PaymentDate =
                    paymentDate,

                PaymentType =
                    PaymentType.Regular,

                Notes =
                    "Concurrent payment B"
            };

        var firstTask =
            scenario.Client
                .PostAsJsonWithIdempotencyAsync(
                    "/api/payments",
                    firstRequest,
                    Guid.NewGuid());

        var secondTask =
            scenario.Client
                .PostAsJsonWithIdempotencyAsync(
                    "/api/payments",
                    secondRequest,
                    Guid.NewGuid());

        await Task.WhenAll(
            firstTask,
            secondTask);

        using var firstResponse =
            await firstTask;

        using var secondResponse =
            await secondTask;

        Assert.Equal(
            HttpStatusCode.Created,
            firstResponse.StatusCode);

        Assert.Equal(
            HttpStatusCode.Created,
            secondResponse.StatusCode);

        var firstPayment =
            await firstResponse.Content
                .ReadFromJsonAsync<PaymentResponse>();

        var secondPayment =
            await secondResponse.Content
                .ReadFromJsonAsync<PaymentResponse>();

        Assert.NotNull(
            firstPayment);

        Assert.NotNull(
            secondPayment);

        Assert.NotEqual(
            firstPayment.Id,
            secondPayment.Id);

        var payments =
            await GetPaymentsAsync(
                scenario.Client,
                scenario.Loan.Id);

        Assert.Equal(
            2,
            payments.Count);

        Assert.Equal(
            200m,
            payments.Sum(
                x => x.Amount));

        var summaryResponse =
            await scenario.Client.GetAsync(
                $"/api/loans/{scenario.Loan.Id}/summary");

        summaryResponse.EnsureSuccessStatusCode();

        var summary =
            await summaryResponse.Content
                .ReadFromJsonAsync<
                    LoanFinancialSummaryResponse>();

        Assert.NotNull(
            summary);

        Assert.Equal(
            200m,
            summary.AmountPaid);

        Assert.Equal(
            1100m,
            summary.Balance);

        Assert.Equal(
            2,
            summary.CompletedInstallments);
    }

    [Fact]
    public async Task
        CreatePayment_DifferentKeysConcurrentFullSettlement_OnlyOneFinancialEffectSucceeds()
    {
        var scenario =
            await CreateScenarioAsync();

        var paymentDate =
            DateTime.UtcNow;

        var request =
            new CreatePaymentRequest
            {
                LoanId =
                    scenario.Loan.Id,

                Amount =
                    1300m,

                PaymentDate =
                    paymentDate,

                PaymentType =
                    PaymentType.FullSettlement,

                Notes =
                    "Concurrent full settlement"
            };

        var firstTask =
            scenario.Client
                .PostAsJsonWithIdempotencyAsync(
                    "/api/payments",
                    request,
                    Guid.NewGuid());

        var secondTask =
            scenario.Client
                .PostAsJsonWithIdempotencyAsync(
                    "/api/payments",
                    request,
                    Guid.NewGuid());

        await Task.WhenAll(
            firstTask,
            secondTask);

        using var firstResponse =
            await firstTask;

        using var secondResponse =
            await secondTask;

        var statusCodes =
            new[]
            {
                firstResponse.StatusCode,
                secondResponse.StatusCode
            };

        Assert.Equal(
            1,
            statusCodes.Count(
                x => x == HttpStatusCode.Created));

        Assert.Equal(
            1,
            statusCodes.Count(
                x => x == HttpStatusCode.BadRequest));

        var payments =
            await GetPaymentsAsync(
                scenario.Client,
                scenario.Loan.Id);

        var storedPayment =
            Assert.Single(
                payments);

        Assert.Equal(
            1300m,
            storedPayment.Amount);

        var summaryResponse =
            await scenario.Client.GetAsync(
                $"/api/loans/{scenario.Loan.Id}/summary");

        summaryResponse.EnsureSuccessStatusCode();

        var summary =
            await summaryResponse.Content
                .ReadFromJsonAsync<
                    LoanFinancialSummaryResponse>();

        Assert.NotNull(
            summary);

        Assert.Equal(
            1300m,
            summary.AmountPaid);

        Assert.Equal(
            0m,
            summary.Balance);

        Assert.Equal(
            LoanStatus.Paid,
            summary.Status);
    }

    [Fact]
    public async Task
        CreatePayment_DifferentKeysConcurrently_WithLateFee_AppliesLateFeeOnlyOnce()
    {
        var scenario =
            await CreateScenarioAsync(
                startDate:
                    DateTime.UtcNow.Date.AddDays(-8),
                lateFeeEnabled:
                    true,
                lateFeeAmount:
                    20m);

        var paymentDate =
            DateTime.UtcNow;

        var firstRequest =
            new CreatePaymentRequest
            {
                LoanId =
                    scenario.Loan.Id,

                Amount =
                    120m,

                PaymentDate =
                    paymentDate,

                PaymentType =
                    PaymentType.Regular,

                Notes =
                    "Concurrent late fee payment A"
            };

        var secondRequest =
            new CreatePaymentRequest
            {
                LoanId =
                    scenario.Loan.Id,

                Amount =
                    120m,

                PaymentDate =
                    paymentDate,

                PaymentType =
                    PaymentType.Regular,

                Notes =
                    "Concurrent late fee payment B"
            };

        var firstTask =
            scenario.Client
                .PostAsJsonWithIdempotencyAsync(
                    "/api/payments",
                    firstRequest,
                    Guid.NewGuid());

        var secondTask =
            scenario.Client
                .PostAsJsonWithIdempotencyAsync(
                    "/api/payments",
                    secondRequest,
                    Guid.NewGuid());

        await Task.WhenAll(
            firstTask,
            secondTask);

        using var firstResponse =
            await firstTask;

        using var secondResponse =
            await secondTask;

        Assert.Equal(
            HttpStatusCode.Created,
            firstResponse.StatusCode);

        Assert.Equal(
            HttpStatusCode.Created,
            secondResponse.StatusCode);

        var firstPayment =
            await firstResponse.Content
                .ReadFromJsonAsync<PaymentResponse>();

        var secondPayment =
            await secondResponse.Content
                .ReadFromJsonAsync<PaymentResponse>();

        Assert.NotNull(
            firstPayment);

        Assert.NotNull(
            secondPayment);

        Assert.NotEqual(
            firstPayment.Id,
            secondPayment.Id);

        var firstReceiptResponse =
            await scenario.Client.GetAsync(
                $"/api/payments/{firstPayment.Id}/receipt");

        var secondReceiptResponse =
            await scenario.Client.GetAsync(
                $"/api/payments/{secondPayment.Id}/receipt");

        firstReceiptResponse.EnsureSuccessStatusCode();
        secondReceiptResponse.EnsureSuccessStatusCode();

        var firstReceipt =
            await firstReceiptResponse.Content
                .ReadFromJsonAsync<
                    PaymentReceiptResponse>();

        var secondReceipt =
            await secondReceiptResponse.Content
                .ReadFromJsonAsync<
                    PaymentReceiptResponse>();

        Assert.NotNull(
            firstReceipt);

        Assert.NotNull(
            secondReceipt);

        var receipts =
            new[]
            {
                firstReceipt,
                secondReceipt
            };

        /*
        * Solo uno de los dos pagos puede encontrar
        * los RD$20 de mora pendientes.
        *
        * Resultado esperado:
        *
        * Payment ganador:
        *   20 -> mora
        *  100 -> contrato
        *
        * Payment siguiente:
        *    0 -> mora
        *  120 -> contrato
        */
        Assert.Equal(
            20m,
            receipts.Sum(
                x => x.LateFeeAmountApplied));

        Assert.Equal(
            220m,
            receipts.Sum(
                x => x.LoanBalanceAmountApplied));

        Assert.Equal(
            new[]
            {
                0m,
                20m
            },
            receipts
                .Select(
                    x => x.LateFeeAmountApplied)
                .OrderBy(
                    x => x)
                .ToArray());

        Assert.Equal(
            new[]
            {
                100m,
                120m
            },
            receipts
                .Select(
                    x => x.LoanBalanceAmountApplied)
                .OrderBy(
                    x => x)
                .ToArray());

        var payments =
            await GetPaymentsAsync(
                scenario.Client,
                scenario.Loan.Id);

        Assert.Equal(
            2,
            payments.Count);

        Assert.Equal(
            240m,
            payments.Sum(
                x => x.Amount));

        var lateFeesResponse =
            await scenario.Client.GetAsync(
                $"/api/late-fees/loans/{scenario.Loan.Id}");

        lateFeesResponse.EnsureSuccessStatusCode();

        var lateFees =
            await lateFeesResponse.Content
                .ReadFromJsonAsync<
                    LateFeeLoanResponse>();

        Assert.NotNull(
            lateFees);

        Assert.Equal(
            0m,
            lateFees.LateFeeBalance);

        var summaryResponse =
            await scenario.Client.GetAsync(
                $"/api/loans/{scenario.Loan.Id}/summary");

        summaryResponse.EnsureSuccessStatusCode();

        var summary =
            await summaryResponse.Content
                .ReadFromJsonAsync<
                    LoanFinancialSummaryResponse>();

        Assert.NotNull(
            summary);

        /*
        * Contractual inicial = 1300
        * Aplicado al contrato = 220
        * Saldo contractual     = 1080
        */
        Assert.Equal(
            1080m,
            summary.Balance);
    }
    [Fact]
    public async Task
        CreatePayment_RetrySameKeyAfterReversal_ReturnsOriginalReversedPaymentWithoutNewEffect()
    {
        var scenario =
            await CreateScenarioAsync();

        var idempotencyKey =
            Guid.NewGuid();

        var request =
            CreatePaymentRequest(
                scenario.Loan.Id,
                100m);

        /*
        * Primera ejecución:
        *
        * se crea Payment A y la operación queda
        * asociada permanentemente al Idempotency-Key.
        */
        using var firstResponse =
            await scenario.Client
                .PostAsJsonWithIdempotencyAsync(
                    "/api/payments",
                    request,
                    idempotencyKey);

        Assert.Equal(
            HttpStatusCode.Created,
            firstResponse.StatusCode);

        var originalPayment =
            await firstResponse.Content
                .ReadFromJsonAsync<PaymentResponse>();

        Assert.NotNull(
            originalPayment);

        Assert.False(
            originalPayment.IsReversed);

        /*
        * Reversamos el pago original.
        */
        using var reversalResponse =
            await scenario.Client
                .PostAsJsonAsync(
                    $"/api/payments/{originalPayment.Id}/reversal",
                    new PaymentReversalRequest
                    {
                        Reason =
                            "Reversal before idempotent retry"
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

        /*
        * Reintentamos exactamente la operación original
        * usando el MISMO Idempotency-Key.
        *
        * No debe crearse otro Payment.
        */
        using var retryResponse =
            await scenario.Client
                .PostAsJsonWithIdempotencyAsync(
                    "/api/payments",
                    request,
                    idempotencyKey);

        Assert.Equal(
            HttpStatusCode.Created,
            retryResponse.StatusCode);

        var retriedPayment =
            await retryResponse.Content
                .ReadFromJsonAsync<PaymentResponse>();

        Assert.NotNull(
            retriedPayment);

        /*
        * Debemos recuperar la misma identidad histórica.
        */
        Assert.Equal(
            originalPayment.Id,
            retriedPayment.Id);

        /*
        * Pero la representación debe reflejar
        * el estado ACTUAL del Payment original.
        */
        Assert.True(
            retriedPayment.IsReversed);

        Assert.Equal(
            "Reversal before idempotent retry",
            retriedPayment.ReversalReason);

        Assert.NotNull(
            retriedPayment.ReversedAt);

        /*
        * Debe seguir existiendo exactamente
        * un único Payment físico.
        */
        var payments =
            await GetPaymentsAsync(
                scenario.Client,
                scenario.Loan.Id);

        var storedPayment =
            Assert.Single(
                payments);

        Assert.Equal(
            originalPayment.Id,
            storedPayment.Id);

        Assert.True(
            storedPayment.IsReversed);

        /*
        * El reverso ya eliminó el efecto financiero.
        * El retry idempotente no debe volver a aplicarlo.
        */
        var summaryResponse =
            await scenario.Client.GetAsync(
                $"/api/loans/{scenario.Loan.Id}/summary");

        summaryResponse.EnsureSuccessStatusCode();

        var summary =
            await summaryResponse.Content
                .ReadFromJsonAsync<
                    LoanFinancialSummaryResponse>();

        Assert.NotNull(
            summary);

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
    // REQUIRED HEADER
    // ============================================================

    [Fact]
    public async Task
        CreatePayment_WithoutIdempotencyKey_ReturnsBadRequestAndPersistsNothing()
    {
        var scenario =
            await CreateScenarioAsync();

        var request =
            CreatePaymentRequest(
                scenario.Loan.Id,
                100m);

        using var message =
            new HttpRequestMessage(
                HttpMethod.Post,
                "/api/payments")
            {
                Content =
                    JsonContent.Create(
                        request)
            };

        using var response =
            await scenario.Client.SendAsync(
                message);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        var payments =
            await GetPaymentsAsync(
                scenario.Client,
                scenario.Loan.Id);

        Assert.Empty(
            payments);
    }

    [Fact]
    public async Task
        CreatePayment_WithInvalidIdempotencyKey_ReturnsBadRequestAndPersistsNothing()
    {
        var scenario =
            await CreateScenarioAsync();

        var request =
            CreatePaymentRequest(
                scenario.Loan.Id,
                100m);

        using var message =
            new HttpRequestMessage(
                HttpMethod.Post,
                "/api/payments")
            {
                Content =
                    JsonContent.Create(
                        request)
            };

        message.Headers.TryAddWithoutValidation(
            "Idempotency-Key",
            "not-a-guid");

        using var response =
            await scenario.Client.SendAsync(
                message);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        var payments =
            await GetPaymentsAsync(
                scenario.Client,
                scenario.Loan.Id);

        Assert.Empty(
            payments);
    }

    // ============================================================
    // TENANT ISOLATION
    // ============================================================

    [Fact]
    public async Task
        CreatePayment_SameKeyAcrossDifferentTenants_IsIndependent()
    {
        var tenant1 =
            await CreateScenarioAsync();

        var tenant2 =
            await CreateScenarioAsync();

        var idempotencyKey =
            Guid.NewGuid();

        var request1 =
            CreatePaymentRequest(
                tenant1.Loan.Id,
                100m);

        var request2 =
            CreatePaymentRequest(
                tenant2.Loan.Id,
                100m);

        var response1 =
            await tenant1.Client
                .PostAsJsonWithIdempotencyAsync(
                    "/api/payments",
                    request1,
                    idempotencyKey);

        var response2 =
            await tenant2.Client
                .PostAsJsonWithIdempotencyAsync(
                    "/api/payments",
                    request2,
                    idempotencyKey);

        Assert.Equal(
            HttpStatusCode.Created,
            response1.StatusCode);

        Assert.Equal(
            HttpStatusCode.Created,
            response2.StatusCode);

        var payment1 =
            await response1.Content
                .ReadFromJsonAsync<PaymentResponse>();

        var payment2 =
            await response2.Content
                .ReadFromJsonAsync<PaymentResponse>();

        Assert.NotNull(
            payment1);

        Assert.NotNull(
            payment2);

        Assert.NotEqual(
            payment1.Id,
            payment2.Id);

        var tenant1Payments =
            await GetPaymentsAsync(
                tenant1.Client,
                tenant1.Loan.Id);

        var tenant2Payments =
            await GetPaymentsAsync(
                tenant2.Client,
                tenant2.Loan.Id);

        Assert.Single(
            tenant1Payments);

        Assert.Single(
            tenant2Payments);
    }

    // ============================================================
    // SCENARIO
    // ============================================================

    private async Task<PaymentScenario>
    CreateScenarioAsync(
        DateTime? startDate = null,
        bool lateFeeEnabled = false,
        decimal lateFeeAmount = 0m)
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
                startDate ??
                    DateTime.UtcNow.Date,
                lateFeeEnabled,
                lateFeeAmount);

        return new PaymentScenario(
            context.Client,
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
                        $"Idempotency Investor {Guid.NewGuid():N}",

                    Phone =
                        "8095551000",

                    Identification =
                        $"IDEMP-INV-{Guid.NewGuid():N}"
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
                        "Idempotency",

                    LastName =
                        $"Client-{Guid.NewGuid():N}",

                    Phone =
                        $"809{Random.Shared.Next(
                            1000000,
                            9999999)}",

                    Address =
                        "Payment idempotency integration test"
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
        DateTime startDate,
        bool lateFeeEnabled,
        decimal lateFeeAmount)
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

                    LateFeeEnabled =
                        lateFeeEnabled,

                    LateFeeCalculationType =
                        LateFeeCalculationType
                            .FixedAmountPerInstallment,

                    LateFeeAmount =
                        lateFeeAmount,

                    LateFeeGraceDays =
                        0,

                    Notes =
                        "Payment idempotency integration test"
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

    private static CreatePaymentRequest
        CreatePaymentRequest(
            Guid loanId,
            decimal amount)
    {
        return new CreatePaymentRequest
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
                "Payment idempotency integration test"
        };
    }

    private static async Task<List<PaymentResponse>>
        GetPaymentsAsync(
            HttpClient client,
            Guid loanId)
    {
        var response =
            await client.GetAsync(
                $"/api/payments?loanId={loanId}");

        response.EnsureSuccessStatusCode();

        var payments =
            await response.Content
                .ReadFromJsonAsync<
                    List<PaymentResponse>>();

        Assert.NotNull(
            payments);

        return payments;
    }

    private sealed record PaymentScenario(
        HttpClient Client,
        LoanResponse Loan);
}