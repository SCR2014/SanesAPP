using System.Net;
using System.Net.Http.Json;
using Sanes.Application.Payments.DTOs;
using Sanes.Domain.Enums;
using Sanes.Web.PaymentReceipts;

namespace Sanes.Web.Tests;

public class PaymentReceiptByPaymentWebServiceTests
{
    [Fact]
    public async Task GetByPaymentAsync_UsesExpectedEndpoint()
    {
        var apiClient =
            new TestSanesApiClient();

        var paymentId =
            Guid.NewGuid();

        var receiptId =
            Guid.NewGuid();

        apiClient.EnqueueResponse(
            JsonResponse(
                CreateReceipt(
                    receiptId,
                    paymentId)));

        var service =
            new PaymentReceiptsWebService(
                apiClient);

        var result =
            await service.GetByPaymentAsync(
                paymentId);

        Assert.NotNull(
            result);

        Assert.Equal(
            receiptId,
            result.Id);

        Assert.Equal(
            paymentId,
            result.PaymentId);

        var request =
            Assert.Single(
                apiClient.Requests);

        Assert.Equal(
            HttpMethod.Get,
            request.Method);

        Assert.Equal(
            $"api/payments/{paymentId:D}/receipt",
            request.Uri);

        Assert.False(
            request.Uri.Contains(
                "tenantId",
                StringComparison.OrdinalIgnoreCase));

        Assert.Null(
            request.Body);
    }

    [Fact]
    public async Task GetByPaymentAsync_NotFound_ReturnsNull()
    {
        var apiClient =
            new TestSanesApiClient();

        var paymentId =
            Guid.NewGuid();

        apiClient.EnqueueResponse(
            new HttpResponseMessage(
                HttpStatusCode.NotFound));

        var service =
            new PaymentReceiptsWebService(
                apiClient);

        var result =
            await service.GetByPaymentAsync(
                paymentId);

        Assert.Null(
            result);

        var request =
            Assert.Single(
                apiClient.Requests);

        Assert.Equal(
            $"api/payments/{paymentId:D}/receipt",
            request.Uri);
    }

    [Fact]
    public async Task GetByPaymentAsync_EmptyId_ThrowsBeforeRequest()
    {
        var apiClient =
            new TestSanesApiClient();

        var service =
            new PaymentReceiptsWebService(
                apiClient);

        await Assert.ThrowsAsync<
            ArgumentException>(
                () =>
                    service.GetByPaymentAsync(
                        Guid.Empty));

        Assert.Empty(
            apiClient.Requests);
    }

    private static PaymentReceiptResponse
        CreateReceipt(
            Guid receiptId,
            Guid paymentId)
    {
        return new PaymentReceiptResponse
        {
            Id =
                receiptId,

            PaymentId =
                paymentId,

            ReceiptNumber =
                "REC-2026-000999",

            ReceiptYear =
                2026,

            SequenceNumber =
                999,

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
                    29,
                    0,
                    30,
                    0,
                    DateTimeKind.Utc),

            PaymentType =
                PaymentType.Regular,

            AmountReceived =
                100m,

            LateFeeAmountApplied =
                0m,

            LoanBalanceAmountApplied =
                100m,

            ContractualBalanceAfter =
                1200m,

            LateFeeBalanceAfter =
                0m,

            TotalOutstandingAfter =
                1200m,

            CreatedAt =
                new DateTime(
                    2026,
                    9,
                    29,
                    0,
                    30,
                    1,
                    DateTimeKind.Utc)
        };
    }

    private static HttpResponseMessage
        JsonResponse<T>(
            T value)
    {
        return new HttpResponseMessage(
            HttpStatusCode.OK)
        {
            Content =
                JsonContent.Create(
                    value)
        };
    }
}