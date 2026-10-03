using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Sanes.Application.FieldCollections.DTOs;
using Sanes.Application.Payments.DTOs;
using Sanes.Application.Payments.Exceptions;
using Sanes.Domain.Enums;
using Sanes.Web.FieldCollections;
using Sanes.Web.Payments;

namespace Sanes.Web.Tests;

public class FieldCollectionsWebServiceTests
{
    [Fact]
    public async Task GetDailyAsync_UsesExpectedEndpointWithoutIdentityParameters()
    {
        var apiClient =
            new TestSanesApiClient();

        var date =
            new DateOnly(
                2026,
                10,
                3);

        apiClient.EnqueueResponse(
            JsonResponse(
                new FieldCollectionDailyResponse
                {
                    Date = date,
                    CollectorName =
                        "Cobrador Test",
                    RoutesCount = 1,
                    ClientsCount = 2,
                    LoansCount = 2,
                    TotalBalance = 1200m,
                    TotalCollectionAmountDue = 100m
                }));

        var service =
            new FieldCollectionsWebService(
                apiClient);

        var result =
            await service.GetDailyAsync(
                date);

        Assert.Equal(
            date,
            result.Date);

        Assert.Equal(
            "Cobrador Test",
            result.CollectorName);

        Assert.Equal(
            1,
            result.RoutesCount);

        var request =
            Assert.Single(
                apiClient.Requests);

        Assert.Equal(
            HttpMethod.Get,
            request.Method);

        Assert.Equal(
            "api/field-collections/daily?date=2026-10-03",
            request.Uri);

        AssertNoIdentityParameters(
            request.Uri);

        Assert.Null(
            request.Body);
    }

    [Fact]
    public async Task CreatePaymentAsync_PostsExpectedPayloadWithoutIdentityFields()
    {
        var apiClient =
            new TestSanesApiClient();

        var routeId =
            Guid.NewGuid();

        var loanId =
            Guid.NewGuid();

        var paymentId =
            Guid.NewGuid();

        var idempotencyKey =
            Guid.NewGuid();

        apiClient.EnqueueResponse(
            JsonResponse(
                new FieldCollectionPaymentResponse
                {
                    PaymentId =
                        paymentId,

                    CollectionRouteId =
                        routeId,

                    LoanId =
                        loanId,

                    Amount =
                        20m,

                    AppliedToLateFees =
                        0m,

                    AppliedToLoan =
                        20m,

                    PaymentType =
                        PaymentType.Partial,

                    BalanceBefore =
                        600m,

                    BalanceAfter =
                        580m,

                    TotalOutstandingBefore =
                        600m,

                    TotalOutstandingAfter =
                        580m
                }));

        var service =
            new FieldCollectionsWebService(
                apiClient);

        var request =
            new CreateFieldCollectionPaymentRequest
            {
                CollectionRouteId =
                    routeId,

                LoanId =
                    loanId,

                Amount =
                    20m,

                PaymentType =
                    PaymentType.Partial,

                Notes =
                    "Pago parcial de prueba"
            };

        var result =
            await service.CreatePaymentAsync(
                request,
                idempotencyKey);

        Assert.Equal(
            paymentId,
            result.PaymentId);

        Assert.Equal(
            20m,
            result.Amount);

        Assert.Equal(
            20m,
            result.AppliedToLoan);

        var recorded =
            Assert.Single(
                apiClient.Requests);

        Assert.Equal(
            HttpMethod.Post,
            recorded.Method);

        Assert.Equal(
            "api/field-collections/payments",
            recorded.Uri);

        Assert.True(
            recorded.Headers.TryGetValue(
                "Idempotency-Key",
                out var idempotencyValues));

        Assert.Single(
            idempotencyValues);

        Assert.Equal(
            idempotencyKey.ToString("D"),
            idempotencyValues[0]);

        AssertNoIdentityParameters(
            recorded.Uri);

        Assert.NotNull(
            recorded.Body);

        using var document =
            JsonDocument.Parse(
                recorded.Body!);

        var root =
            document.RootElement;

        Assert.Equal(
            routeId,
            root
                .GetProperty(
                    "collectionRouteId")
                .GetGuid());

        Assert.Equal(
            loanId,
            root
                .GetProperty(
                    "loanId")
                .GetGuid());

        Assert.Equal(
            20m,
            root
                .GetProperty(
                    "amount")
                .GetDecimal());

        Assert.Equal(
            (int)PaymentType.Partial,
            root
                .GetProperty(
                    "paymentType")
                .GetInt32());

        Assert.Equal(
            "Pago parcial de prueba",
            root
                .GetProperty(
                    "notes")
                .GetString());

        Assert.False(
            recorded.Body!.Contains(
                "tenantId",
                StringComparison.OrdinalIgnoreCase));

        Assert.False(
            recorded.Body.Contains(
                "appUserId",
                StringComparison.OrdinalIgnoreCase));

        Assert.False(
            recorded.Body.Contains(
                "collectedByAppUserId",
                StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task
        CreatePaymentAsync_Conflict_ThrowsPaymentIdempotencyConflictException()
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
            new FieldCollectionsWebService(
                apiClient);

        await Assert.ThrowsAsync<
            PaymentIdempotencyConflictException>(
                () =>
                    service.CreatePaymentAsync(
                        new CreateFieldCollectionPaymentRequest
                        {
                            CollectionRouteId =
                                Guid.NewGuid(),

                            LoanId =
                                Guid.NewGuid(),

                            Amount =
                                100m,

                            PaymentType =
                                PaymentType.Regular
                        },
                        Guid.NewGuid()));
    }

    [Fact]
    public async Task CreatePaymentAsync_BadRequest_TranslatesPaymentRule()
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
                                "A partial payment must apply less than one installment amount to the loan balance."
                        })
            });

        var service =
            new FieldCollectionsWebService(
                apiClient);

        var request =
            new CreateFieldCollectionPaymentRequest
            {
                CollectionRouteId =
                    Guid.NewGuid(),

                LoanId =
                    Guid.NewGuid(),

                Amount =
                    50m,

                PaymentType =
                    PaymentType.Partial
            };

        var exception =
            await Assert.ThrowsAsync<
                ArgumentException>(
                () =>
                    service.CreatePaymentAsync(
                        request,
                        Guid.NewGuid()));

        Assert.Equal(
            "Un pago parcial debe aplicar al saldo del préstamo menos del monto de una cuota.",
            exception.Message);

        var recorded =
            Assert.Single(
                apiClient.Requests);

        Assert.Equal(
            HttpMethod.Post,
            recorded.Method);

        Assert.Equal(
            "api/field-collections/payments",
            recorded.Uri);
    }

    [Fact]
    public async Task GetReceiptByPaymentAsync_ReturnsPersistentReceipt()
    {
        var apiClient =
            new TestSanesApiClient();

        var paymentId =
            Guid.NewGuid();

        var receiptId =
            Guid.NewGuid();

        apiClient.EnqueueResponse(
            JsonResponse(
                new PaymentReceiptResponse
                {
                    Id =
                        receiptId,

                    PaymentId =
                        paymentId,

                    ReceiptNumber =
                        "REC-2026-000123",

                    ReceiptYear =
                        2026,

                    SequenceNumber =
                        123,

                    TenantName =
                        "Sanes Test",

                    TenantLegalName =
                        "Sanes Test SRL",

                    CurrencyCode =
                        "DOP",

                    CurrencySymbol =
                        "RD$",

                    ClientId =
                        Guid.NewGuid(),

                    ClientName =
                        "Cliente Test",

                    LoanId =
                        Guid.NewGuid(),

                    PaymentDate =
                        new DateTime(
                            2026,
                            9,
                            27,
                            15,
                            0,
                            0,
                            DateTimeKind.Utc),

                    PaymentType =
                        PaymentType.Regular,

                    AmountReceived =
                        50m,

                    LateFeeAmountApplied =
                        0m,

                    LoanBalanceAmountApplied =
                        50m,

                    ContractualBalanceAfter =
                        600m,

                    LateFeeBalanceAfter =
                        0m,

                    TotalOutstandingAfter =
                        600m,

                    CollectedByAppUserId =
                        Guid.NewGuid(),

                    CollectedByName =
                        "Cobrador Test"
                }));

        var service =
            new FieldCollectionsWebService(
                apiClient);

        var result =
            await service
                .GetReceiptByPaymentAsync(
                    paymentId);

        Assert.NotNull(
            result);

        Assert.Equal(
            receiptId,
            result.Id);

        Assert.Equal(
            paymentId,
            result.PaymentId);

        Assert.Equal(
            "REC-2026-000123",
            result.ReceiptNumber);

        Assert.Equal(
            "RD$",
            result.CurrencySymbol);

        Assert.Equal(
            50m,
            result.AmountReceived);

        var request =
            Assert.Single(
                apiClient.Requests);

        Assert.Equal(
            HttpMethod.Get,
            request.Method);

        Assert.Equal(
            $"api/payments/{paymentId:D}/receipt",
            request.Uri);

        AssertNoIdentityParameters(
            request.Uri);
    }

    [Fact]
    public async Task GetReceiptByPaymentAsync_NotFound_ReturnsNull()
    {
        var apiClient =
            new TestSanesApiClient();

        var paymentId =
            Guid.NewGuid();

        apiClient.EnqueueResponse(
            new HttpResponseMessage(
                HttpStatusCode.NotFound));

        var service =
            new FieldCollectionsWebService(
                apiClient);

        var result =
            await service
                .GetReceiptByPaymentAsync(
                    paymentId);

        Assert.Null(
            result);

        var request =
            Assert.Single(
                apiClient.Requests);

        Assert.Equal(
            HttpMethod.Get,
            request.Method);

        Assert.Equal(
            $"api/payments/{paymentId:D}/receipt",
            request.Uri);

        AssertNoIdentityParameters(
            request.Uri);
    }

    private static void AssertNoIdentityParameters(
        string value)
    {
        Assert.False(
            value.Contains(
                "tenantId",
                StringComparison.OrdinalIgnoreCase));

        Assert.False(
            value.Contains(
                "appUserId",
                StringComparison.OrdinalIgnoreCase));

        Assert.False(
            value.Contains(
                "collectorId",
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