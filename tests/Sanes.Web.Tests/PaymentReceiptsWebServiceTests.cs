using System.Net;
using System.Net.Http.Json;
using Sanes.Application.Payments.DTOs;
using Sanes.Domain.Enums;
using Sanes.Web.PaymentReceipts;

namespace Sanes.Web.Tests;

public class PaymentReceiptsWebServiceTests
{
    [Fact]
    public async Task GetPagedAsync_WithoutFilters_UsesPagingOnly()
    {
        var apiClient =
            new TestSanesApiClient();

        apiClient.EnqueueResponse(
            JsonResponse(
                new PaymentReceiptListResponse
                {
                    Page = 1,
                    PageSize = 25,
                    TotalCount = 0,
                    Items = []
                }));

        var service =
            new PaymentReceiptsWebService(
                apiClient);

        var result =
            await service.GetPagedAsync(
                new PaymentReceiptListRequest
                {
                    Page = 1,
                    PageSize = 25
                });

        Assert.Equal(
            1,
            result.Page);

        Assert.Equal(
            25,
            result.PageSize);

        Assert.Equal(
            0,
            result.TotalCount);

        var request =
            Assert.Single(
                apiClient.Requests);

        Assert.Equal(
            HttpMethod.Get,
            request.Method);

        Assert.Equal(
            "api/payment-receipts?page=1&pageSize=25",
            request.Uri);

        AssertNoTenantParameter(
            request.Uri);

        Assert.Null(
            request.Body);
    }

    [Fact]
    public async Task GetPagedAsync_WithFilters_BuildsExpectedQuery()
    {
        var apiClient =
            new TestSanesApiClient();

        var collectorId =
            Guid.NewGuid();

        var receiptId =
            Guid.NewGuid();

        apiClient.EnqueueResponse(
            JsonResponse(
                new PaymentReceiptListResponse
                {
                    Page = 2,
                    PageSize = 25,
                    TotalCount = 26,
                    Items =
                    [
                        CreateReceipt(
                            receiptId)
                    ]
                }));

        var service =
            new PaymentReceiptsWebService(
                apiClient);

        var result =
            await service.GetPagedAsync(
                new PaymentReceiptListRequest
                {
                    From =
                        new DateOnly(
                            2026,
                            9,
                            1),

                    To =
                        new DateOnly(
                            2026,
                            9,
                            30),

                    Search =
                        "  REC 2026/0001  ",

                    CollectorId =
                        collectorId,

                    Page =
                        2,

                    PageSize =
                        25
                });

        Assert.Equal(
            26,
            result.TotalCount);

        var receipt =
            Assert.Single(
                result.Items);

        Assert.Equal(
            receiptId,
            receipt.Id);

        var request =
            Assert.Single(
                apiClient.Requests);

        Assert.Equal(
            HttpMethod.Get,
            request.Method);

        Assert.Equal(
            "api/payment-receipts" +
            "?page=2" +
            "&pageSize=25" +
            "&from=2026-09-01" +
            "&to=2026-09-30" +
            "&search=REC%202026%2F0001" +
            $"&collectorId={collectorId:D}",
            request.Uri);

        AssertNoTenantParameter(
            request.Uri);

        Assert.Null(
            request.Body);
    }

    [Fact]
    public async Task GetPagedAsync_BadRequest_TranslatesDateValidation()
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
                                "From date cannot be after To date."
                        })
            });

        var service =
            new PaymentReceiptsWebService(
                apiClient);

        var exception =
            await Assert.ThrowsAsync<
                ArgumentException>(
                () =>
                    service.GetPagedAsync(
                        new PaymentReceiptListRequest
                        {
                            From =
                                new DateOnly(
                                    2026,
                                    9,
                                    30),

                            To =
                                new DateOnly(
                                    2026,
                                    9,
                                    1),

                            Page =
                                1,

                            PageSize =
                                25
                        }));

        Assert.Equal(
            "La fecha desde no puede ser posterior a la fecha hasta.",
            exception.Message);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsReceipt()
    {
        var apiClient =
            new TestSanesApiClient();

        var receiptId =
            Guid.NewGuid();

        apiClient.EnqueueResponse(
            JsonResponse(
                CreateReceipt(
                    receiptId)));

        var service =
            new PaymentReceiptsWebService(
                apiClient);

        var result =
            await service.GetByIdAsync(
                receiptId);

        Assert.NotNull(
            result);

        Assert.Equal(
            receiptId,
            result.Id);

        var request =
            Assert.Single(
                apiClient.Requests);

        Assert.Equal(
            HttpMethod.Get,
            request.Method);

        Assert.Equal(
            $"api/payment-receipts/{receiptId:D}",
            request.Uri);

        AssertNoTenantParameter(
            request.Uri);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        var apiClient =
            new TestSanesApiClient();

        var receiptId =
            Guid.NewGuid();

        apiClient.EnqueueResponse(
            new HttpResponseMessage(
                HttpStatusCode.NotFound));

        var service =
            new PaymentReceiptsWebService(
                apiClient);

        var result =
            await service.GetByIdAsync(
                receiptId);

        Assert.Null(
            result);

        var request =
            Assert.Single(
                apiClient.Requests);

        Assert.Equal(
            $"api/payment-receipts/{receiptId:D}",
            request.Uri);
    }

    [Fact]
    public async Task GetByIdAsync_EmptyId_ThrowsBeforeSendingRequest()
    {
        var apiClient =
            new TestSanesApiClient();

        var service =
            new PaymentReceiptsWebService(
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
    public async Task GetByNumberAsync_TrimsAndEncodesReceiptNumber()
    {
        var apiClient =
            new TestSanesApiClient();

        var receipt =
            CreateReceipt(
                Guid.NewGuid());

        apiClient.EnqueueResponse(
            JsonResponse(
                receipt));

        var service =
            new PaymentReceiptsWebService(
                apiClient);

        var result =
            await service.GetByNumberAsync(
                "  REC 2026/000123  ");

        Assert.NotNull(
            result);

        Assert.Equal(
            receipt.Id,
            result.Id);

        var request =
            Assert.Single(
                apiClient.Requests);

        Assert.Equal(
            HttpMethod.Get,
            request.Method);

        Assert.Equal(
            "api/payment-receipts/by-number/" +
            "REC%202026%2F000123",
            request.Uri);

        AssertNoTenantParameter(
            request.Uri);
    }

    [Fact]
    public async Task GetByNumberAsync_NotFound_ReturnsNull()
    {
        var apiClient =
            new TestSanesApiClient();

        apiClient.EnqueueResponse(
            new HttpResponseMessage(
                HttpStatusCode.NotFound));

        var service =
            new PaymentReceiptsWebService(
                apiClient);

        var result =
            await service.GetByNumberAsync(
                "REC-2026-999999");

        Assert.Null(
            result);

        var request =
            Assert.Single(
                apiClient.Requests);

        Assert.Equal(
            "api/payment-receipts/by-number/" +
            "REC-2026-999999",
            request.Uri);
    }

    [Fact]
    public async Task GetByNumberAsync_EmptyNumber_ThrowsBeforeSendingRequest()
    {
        var apiClient =
            new TestSanesApiClient();

        var service =
            new PaymentReceiptsWebService(
                apiClient);

        await Assert.ThrowsAsync<
            ArgumentException>(
            () =>
                service.GetByNumberAsync(
                    "   "));

        Assert.Empty(
            apiClient.Requests);
    }

    private static PaymentReceiptResponse
        CreateReceipt(
            Guid receiptId)
    {
        return new PaymentReceiptResponse
        {
            Id =
                receiptId,

            PaymentId =
                Guid.NewGuid(),

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
                "Cobrador Test",

            CreatedAt =
                new DateTime(
                    2026,
                    9,
                    27,
                    15,
                    0,
                    1,
                    DateTimeKind.Utc)
        };
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