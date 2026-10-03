using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Sanes.Application.Loans.DTOs;
using Sanes.Application.Payments.DTOs;
using Sanes.Application.Payments.Exceptions;
using Sanes.Domain.Enums;
using Sanes.Web.Payments;

namespace Sanes.Web.Tests;

public class PaymentsWebServiceTests
{
    [Fact]
    public async Task GetByLoanAsync_UsesExpectedEndpoint()
    {
        var apiClient =
            new TestSanesApiClient();

        var loanId =
            Guid.NewGuid();

        var paymentId =
            Guid.NewGuid();

        apiClient.EnqueueResponse(
            JsonResponse(
                new List<PaymentResponse>
                {
                    new()
                    {
                        Id =
                            paymentId,

                        LoanId =
                            loanId,

                        Amount =
                            100m,

                        PaymentDate =
                            DateTime.UtcNow,

                        PaymentType =
                            PaymentType.Regular
                    }
                }));

        var service =
            new PaymentsWebService(
                apiClient);

        var result =
            await service.GetByLoanAsync(
                loanId);

        var payment =
            Assert.Single(
                result);

        Assert.Equal(
            paymentId,
            payment.Id);

        Assert.Equal(
            loanId,
            payment.LoanId);

        var request =
            Assert.Single(
                apiClient.Requests);

        Assert.Equal(
            HttpMethod.Get,
            request.Method);

        Assert.Equal(
            $"api/payments?loanId={loanId:D}",
            request.Uri);

        AssertNoTenantParameter(
            request.Uri);

        Assert.Null(
            request.Body);
    }

    [Fact]
    public async Task GetByLoanAsync_EmptyId_ThrowsBeforeRequest()
    {
        var apiClient =
            new TestSanesApiClient();

        var service =
            new PaymentsWebService(
                apiClient);

        await Assert.ThrowsAsync<
            ArgumentException>(
                () =>
                    service.GetByLoanAsync(
                        Guid.Empty));

        Assert.Empty(
            apiClient.Requests);
    }

    [Fact]
    public async Task GetByIdAsync_UsesExpectedEndpoint()
    {
        var apiClient =
            new TestSanesApiClient();

        var paymentId =
            Guid.NewGuid();

        var loanId =
            Guid.NewGuid();

        apiClient.EnqueueResponse(
            JsonResponse(
                new PaymentResponse
                {
                    Id =
                        paymentId,

                    LoanId =
                        loanId,

                    Amount =
                        50m,

                    PaymentDate =
                        DateTime.UtcNow,

                    PaymentType =
                        PaymentType.Partial
                }));

        var service =
            new PaymentsWebService(
                apiClient);

        var result =
            await service.GetByIdAsync(
                paymentId);

        Assert.NotNull(
            result);

        Assert.Equal(
            paymentId,
            result.Id);

        var request =
            Assert.Single(
                apiClient.Requests);

        Assert.Equal(
            HttpMethod.Get,
            request.Method);

        Assert.Equal(
            $"api/payments/{paymentId:D}",
            request.Uri);

        AssertNoTenantParameter(
            request.Uri);

        Assert.Null(
            request.Body);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        var apiClient =
            new TestSanesApiClient();

        var paymentId =
            Guid.NewGuid();

        apiClient.EnqueueResponse(
            new HttpResponseMessage(
                HttpStatusCode.NotFound));

        var service =
            new PaymentsWebService(
                apiClient);

        var result =
            await service.GetByIdAsync(
                paymentId);

        Assert.Null(
            result);

        var request =
            Assert.Single(
                apiClient.Requests);

        Assert.Equal(
            $"api/payments/{paymentId:D}",
            request.Uri);
    }

    [Fact]
    public async Task GetByIdAsync_EmptyId_ThrowsBeforeRequest()
    {
        var apiClient =
            new TestSanesApiClient();

        var service =
            new PaymentsWebService(
                apiClient);

        await Assert.ThrowsAsync<
            ArgumentException>(
                () =>
                    service.GetByIdAsync(
                        Guid.Empty));

        Assert.Empty(
            apiClient.Requests);
    }

    [Fact]
    public async Task CreateAsync_UsesExpectedEndpointAndBody()
    {
        var apiClient =
            new TestSanesApiClient();

        var loanId =
            Guid.NewGuid();

        var paymentId =
            Guid.NewGuid();

        var idempotencyKey =
            Guid.NewGuid();

        var paymentDate =
            new DateTime(
                2026,
                9,
                29,
                0,
                30,
                0,
                DateTimeKind.Utc);

        apiClient.EnqueueResponse(
            JsonResponse(
                new PaymentResponse
                {
                    Id =
                        paymentId,

                    LoanId =
                        loanId,

                    Amount =
                        100m,

                    PaymentDate =
                        paymentDate,

                    PaymentType =
                        PaymentType.Regular,

                    Notes =
                        "Pago administrativo"
                },
                HttpStatusCode.Created));

        var service =
            new PaymentsWebService(
                apiClient);

        var result =
            await service.CreateAsync(
                new CreatePaymentRequest
                {
                    LoanId =
                        loanId,

                    Amount =
                        100m,

                    PaymentDate =
                        paymentDate,

                    PaymentType =
                        PaymentType.Regular,

                    Notes =
                        "Pago administrativo",

                    CollectedByAppUserId =
                        null,

                    CollectionRouteId =
                        null
                },
                idempotencyKey);

        Assert.Equal(
            paymentId,
            result.Id);

        var request =
            Assert.Single(
                apiClient.Requests);

        Assert.Equal(
            HttpMethod.Post,
            request.Method);

        Assert.Equal(
            "api/payments",
            request.Uri);

        Assert.True(
            request.Headers.TryGetValue(
                "Idempotency-Key",
                out var idempotencyValues));

        Assert.Single(
            idempotencyValues);

        Assert.Equal(
            idempotencyKey.ToString("D"),
            idempotencyValues[0]);

        Assert.NotNull(
            request.Body);

        using var document =
            JsonDocument.Parse(
                request.Body);

        var root =
            document.RootElement;

        Assert.Equal(
            loanId,
            root
                .GetProperty("loanId")
                .GetGuid());

        Assert.Equal(
            100m,
            root
                .GetProperty("amount")
                .GetDecimal());

        Assert.Equal(
            (int)PaymentType.Regular,
            root
                .GetProperty("paymentType")
                .GetInt32());

        Assert.Equal(
            "Pago administrativo",
            root
                .GetProperty("notes")
                .GetString());

        Assert.Equal(
            JsonValueKind.Null,
            root
                .GetProperty(
                    "collectedByAppUserId")
                .ValueKind);

        Assert.Equal(
            JsonValueKind.Null,
            root
                .GetProperty(
                    "collectionRouteId")
                .ValueKind);

        Assert.False(
            root.TryGetProperty(
                "tenantId",
                out _));
    }

    [Fact]
    public async Task
        CreateAsync_Conflict_ThrowsPaymentIdempotencyConflictException()
    {
        var apiClient =
            new TestSanesApiClient();

        apiClient.EnqueueResponse(
            new HttpResponseMessage(
                HttpStatusCode.Conflict)
            {
                Content =
                    JsonContent.Create(
                        new
                        {
                            message =
                                "The idempotency key was already used for a different payment operation."
                        })
            });

        var service =
            new PaymentsWebService(
                apiClient);

        await Assert.ThrowsAsync<
            PaymentIdempotencyConflictException>(
                () =>
                    service.CreateAsync(
                        new CreatePaymentRequest
                        {
                            LoanId =
                                Guid.NewGuid(),

                            Amount =
                                100m,

                            PaymentDate =
                                DateTime.UtcNow,

                            PaymentType =
                                PaymentType.Regular
                        },
                        Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateAsync_BadRequest_PreservesTranslatedMessage()
    {
        var apiClient =
            new TestSanesApiClient();

        apiClient.EnqueueResponse(
            new HttpResponseMessage(
                HttpStatusCode.BadRequest)
            {
                Content =
                    JsonContent.Create(
                        new
                        {
                            message =
                                "Payments can only be registered for active loans."
                        })
            });

        var service =
            new PaymentsWebService(
                apiClient);

        var exception =
            await Assert.ThrowsAsync<
                ArgumentException>(
                    () =>
                        service.CreateAsync(
                            new CreatePaymentRequest
                            {
                                LoanId =
                                    Guid.NewGuid(),

                                Amount =
                                    100m,

                                PaymentDate =
                                    DateTime.UtcNow,

                                PaymentType =
                                    PaymentType.Regular
                            },
                            Guid.NewGuid()));

        Assert.Equal(
            "Solo se pueden registrar pagos en préstamos activos.",
            exception.Message);
    }

    [Fact]
    public async Task ReverseAsync_UsesExpectedEndpointAndBody()
    {
        var apiClient =
            new TestSanesApiClient();

        var paymentId =
            Guid.NewGuid();

        var reversalId =
            Guid.NewGuid();

        var loanId =
            Guid.NewGuid();

        var reversedByAppUserId =
            Guid.NewGuid();

        apiClient.EnqueueResponse(
            JsonResponse(
                new PaymentReversalResponse
                {
                    Id =
                        reversalId,

                    PaymentId =
                        paymentId,

                    LoanId =
                        loanId,

                    Amount =
                        100m,

                    PaymentDate =
                        DateTime.UtcNow,

                    PaymentType =
                        PaymentType.Regular,

                    ReversedByAppUserId =
                        reversedByAppUserId,

                    Reason =
                        "Pago duplicado",

                    CreatedAt =
                        DateTime.UtcNow
                }));

        var service =
            new PaymentsWebService(
                apiClient);

        var result =
            await service.ReverseAsync(
                paymentId,
                new PaymentReversalRequest
                {
                    Reason =
                        "  Pago duplicado  "
                });

        Assert.Equal(
            reversalId,
            result.Id);

        Assert.Equal(
            paymentId,
            result.PaymentId);

        var request =
            Assert.Single(
                apiClient.Requests);

        Assert.Equal(
            HttpMethod.Post,
            request.Method);

        Assert.Equal(
            $"api/payments/{paymentId:D}/reversal",
            request.Uri);

        AssertNoTenantParameter(
            request.Uri);

        Assert.NotNull(
            request.Body);

        using var document =
            JsonDocument.Parse(
                request.Body);

        var root =
            document.RootElement;

        Assert.Equal(
            "Pago duplicado",
            root
                .GetProperty("reason")
                .GetString());

        Assert.False(
            root.TryGetProperty(
                "tenantId",
                out _));

        Assert.False(
            root.TryGetProperty(
                "appUserId",
                out _));

        Assert.False(
            root.TryGetProperty(
                "loanId",
                out _));

        Assert.False(
            root.TryGetProperty(
                "amount",
                out _));
    }

    [Fact]
    public async Task ReverseAsync_EmptyPaymentId_ThrowsBeforeRequest()
    {
        var apiClient =
            new TestSanesApiClient();

        var service =
            new PaymentsWebService(
                apiClient);

        await Assert.ThrowsAsync<
            ArgumentException>(
            () =>
                service.ReverseAsync(
                    Guid.Empty,
                    new PaymentReversalRequest
                    {
                        Reason =
                            "Pago duplicado"
                    }));

        Assert.Empty(
            apiClient.Requests);
    }

    [Fact]
    public async Task ReverseAsync_EmptyReason_ThrowsBeforeRequest()
    {
        var apiClient =
            new TestSanesApiClient();

        var service =
            new PaymentsWebService(
                apiClient);

        await Assert.ThrowsAsync<
            ArgumentException>(
            () =>
                service.ReverseAsync(
                    Guid.NewGuid(),
                    new PaymentReversalRequest
                    {
                        Reason =
                            "   "
                    }));

        Assert.Empty(
            apiClient.Requests);
    }

    [Theory]
    [InlineData(
        "The payment has already been reversed.",
        "El pago ya fue reversado.")]
    [InlineData(
        "Only the latest effective payment of the loan can be reversed.",
        "Solo se puede reversar el último pago vigente del préstamo.")]
    [InlineData(
        "Payments generated by an early settlement cannot be reversed from the payment reversal module.",
        "Los pagos generados por una liquidación anticipada no pueden reversarse desde este módulo.")]
    public async Task ReverseAsync_BadRequest_TranslatesMessage(
        string apiMessage,
        string expectedMessage)
    {
        var apiClient =
            new TestSanesApiClient();

        apiClient.EnqueueResponse(
            new HttpResponseMessage(
                HttpStatusCode.BadRequest)
            {
                Content =
                    JsonContent.Create(
                        new
                        {
                            message =
                                apiMessage
                        })
            });

        var service =
            new PaymentsWebService(
                apiClient);

        var exception =
            await Assert.ThrowsAsync<
                ArgumentException>(
                () =>
                    service.ReverseAsync(
                        Guid.NewGuid(),
                        new PaymentReversalRequest
                        {
                            Reason =
                                "Corrección administrativa"
                        }));

        Assert.Equal(
            expectedMessage,
            exception.Message);
    }

    [Fact]
    public void PaymentFormModel_ToRequest_UsesCurrentUtcTime()
    {
        var model =
            new PaymentFormModel
            {
                LoanId =
                    Guid.NewGuid(),

                Amount =
                    75m,

                PaymentType =
                    PaymentType.Partial,

                Notes =
                    "  Pago desde Web  "
            };

        var before =
            DateTime.UtcNow;

        var request =
            model.ToRequest();

        var after =
            DateTime.UtcNow;

        Assert.Equal(
            model.LoanId,
            request.LoanId);

        Assert.Equal(
            75m,
            request.Amount);

        Assert.Equal(
            PaymentType.Partial,
            request.PaymentType);

        Assert.Equal(
            "Pago desde Web",
            request.Notes);

        Assert.Null(
            request.CollectedByAppUserId);

        Assert.Null(
            request.CollectionRouteId);

        Assert.Equal(
            DateTimeKind.Utc,
            request.PaymentDate.Kind);

        Assert.InRange(
            request.PaymentDate,
            before,
            after);
    }

    [Fact]
    public void PaymentFormModel_ForLoan_UsesNextInstallmentAmount()
    {
        var loanId =
            Guid.NewGuid();

        var model =
            PaymentFormModel.ForLoan(
                loanId,
                new LoanFinancialSummaryResponse
                {
                    NextInstallmentAmountDue =
                        100m,

                    TotalOutstanding =
                        1300m,

                    IsOverdue =
                        false
                });

        Assert.Equal(
            loanId,
            model.LoanId);

        Assert.Equal(
            100m,
            model.Amount);

        Assert.Equal(
            PaymentType.Regular,
            model.PaymentType);
    }

    [Fact]
    public void PaymentFormModel_ForLoan_WhenOverdue_UsesTotalOverdueAmountDue()
    {
        var model =
            PaymentFormModel.ForLoan(
                Guid.NewGuid(),
                new LoanFinancialSummaryResponse
                {
                    IsOverdue =
                        true,

                    TotalOverdueAmountDue =
                        220m,

                    NextInstallmentAmountDue =
                        100m,

                    TotalOutstanding =
                        900m
                });

        Assert.Equal(
            220m,
            model.Amount);
    }

    [Fact]
    public void PaymentFormModel_ForLoan_FallsBackToTotalOutstanding()
    {
        var model =
            PaymentFormModel.ForLoan(
                Guid.NewGuid(),
                new LoanFinancialSummaryResponse
                {
                    IsOverdue =
                        false,

                    NextInstallmentAmountDue =
                        0m,

                    TotalOutstanding =
                        85m
                });

        Assert.Equal(
            85m,
            model.Amount);
    }

    private static void AssertNoTenantParameter(
        string value)
    {
        Assert.False(
            value.Contains(
                "tenantId",
                StringComparison.OrdinalIgnoreCase));
    }

    private static HttpResponseMessage
        JsonResponse<T>(
            T value,
            HttpStatusCode statusCode =
                HttpStatusCode.OK)
    {
        return new HttpResponseMessage(
            statusCode)
        {
            Content =
                JsonContent.Create(
                    value)
        };
    }
}