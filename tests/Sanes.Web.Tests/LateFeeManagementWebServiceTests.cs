using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Sanes.Application.LateFees.DTOs;
using Sanes.Domain.Enums;
using Sanes.Web.LateFees;

namespace Sanes.Web.Tests;

public class LateFeeManagementWebServiceTests
{
    [Fact]
    public async Task GetByLoanAsync_UsesExpectedEndpoint()
    {
        var apiClient =
            new TestSanesApiClient();

        var loanId =
            Guid.NewGuid();

        apiClient.EnqueueResponse(
            JsonResponse(
                new LateFeeLoanResponse
                {
                    LoanId =
                        loanId,

                    TotalOriginalCharges =
                        40m,

                    TotalAdjustedCharges =
                        35m,

                    TotalPaidToLateFees =
                        10m,

                    LateFeeBalance =
                        25m,

                    Charges =
                    [
                        new LateFeeChargeResponse
                        {
                            Id =
                                Guid.NewGuid(),

                            LoanId =
                                loanId,

                            InstallmentNumber =
                                1,

                            CalculationType =
                                LateFeeCalculationType
                                    .FixedAmountPerInstallment,

                            OriginalAmount =
                                20m,

                            AdjustedAmount =
                                15m,

                            PaidAmount =
                                0m,

                            OutstandingAmount =
                                15m
                        }
                    ]
                }));

        var service =
            new LateFeeManagementWebService(
                apiClient);

        var result =
            await service.GetByLoanAsync(
                loanId);

        Assert.NotNull(
            result);

        Assert.Equal(
            loanId,
            result.LoanId);

        Assert.Equal(
            25m,
            result.LateFeeBalance);

        var request =
            Assert.Single(
                apiClient.Requests);

        Assert.Equal(
            HttpMethod.Get,
            request.Method);

        Assert.Equal(
            $"api/late-fees/loans/{loanId:D}",
            request.Uri);

        AssertNoTenantOrUser(
            request.Uri);

        Assert.Null(
            request.Body);
    }

    [Fact]
    public async Task GetByLoanAsync_NotFound_ReturnsNull()
    {
        var apiClient =
            new TestSanesApiClient();

        apiClient.EnqueueResponse(
            new HttpResponseMessage(
                HttpStatusCode.NotFound));

        var service =
            new LateFeeManagementWebService(
                apiClient);

        var result =
            await service.GetByLoanAsync(
                Guid.NewGuid());

        Assert.Null(
            result);
    }

    [Fact]
    public async Task GetByLoanAsync_EmptyLoanId_ThrowsBeforeRequest()
    {
        var apiClient =
            new TestSanesApiClient();

        var service =
            new LateFeeManagementWebService(
                apiClient);

        var exception =
            await Assert.ThrowsAsync<
                ArgumentException>(
                    () =>
                        service.GetByLoanAsync(
                            Guid.Empty));

        Assert.Contains(
            "préstamo",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        Assert.Empty(
            apiClient.Requests);
    }

    [Fact]
    public async Task AddAdjustmentAsync_Waiver_UsesExpectedEndpointAndBody()
    {
        var apiClient =
            new TestSanesApiClient();

        var chargeId =
            Guid.NewGuid();

        var loanId =
            Guid.NewGuid();

        apiClient.EnqueueResponse(
            JsonResponse(
                CreateChargeResponse(
                    chargeId,
                    loanId,
                    waivedAmount: 5m,
                    adjustedAmount: 15m,
                    outstandingAmount: 15m),
                HttpStatusCode.Created));

        var service =
            new LateFeeManagementWebService(
                apiClient);

        var result =
            await service.AddAdjustmentAsync(
                chargeId,
                new LateFeeAdjustmentRequest
                {
                    AdjustmentType =
                        LateFeeAdjustmentType
                            .Waiver,

                    Amount =
                        5m,

                    Reason =
                        "Condonación autorizada"
                });

        Assert.NotNull(
            result);

        Assert.Equal(
            5m,
            result.WaivedAmount);

        Assert.Equal(
            15m,
            result.OutstandingAmount);

        var request =
            Assert.Single(
                apiClient.Requests);

        Assert.Equal(
            HttpMethod.Post,
            request.Method);

        Assert.Equal(
            $"api/late-fees/charges/{chargeId:D}/adjustments",
            request.Uri);

        AssertAdjustmentRequest(
            request,
            LateFeeAdjustmentType.Waiver,
            5m,
            "Condonación autorizada");
    }

    [Fact]
    public async Task AddAdjustmentAsync_Decrease_UsesExpectedBody()
    {
        var apiClient =
            new TestSanesApiClient();

        var chargeId =
            Guid.NewGuid();

        var loanId =
            Guid.NewGuid();

        apiClient.EnqueueResponse(
            JsonResponse(
                CreateChargeResponse(
                    chargeId,
                    loanId,
                    decreaseAmount: 4m,
                    adjustedAmount: 16m,
                    outstandingAmount: 16m),
                HttpStatusCode.Created));

        var service =
            new LateFeeManagementWebService(
                apiClient);

        var result =
            await service.AddAdjustmentAsync(
                chargeId,
                new LateFeeAdjustmentRequest
                {
                    AdjustmentType =
                        LateFeeAdjustmentType
                            .Decrease,

                    Amount =
                        4m,

                    Reason =
                        "Corrección administrativa"
                });

        Assert.NotNull(
            result);

        Assert.Equal(
            4m,
            result.DecreaseAmount);

        Assert.Equal(
            16m,
            result.OutstandingAmount);

        var request =
            Assert.Single(
                apiClient.Requests);

        AssertAdjustmentRequest(
            request,
            LateFeeAdjustmentType.Decrease,
            4m,
            "Corrección administrativa");
    }

    [Fact]
    public async Task AddAdjustmentAsync_Increase_UsesExpectedBody()
    {
        var apiClient =
            new TestSanesApiClient();

        var chargeId =
            Guid.NewGuid();

        var loanId =
            Guid.NewGuid();

        apiClient.EnqueueResponse(
            JsonResponse(
                CreateChargeResponse(
                    chargeId,
                    loanId,
                    increaseAmount: 10m,
                    adjustedAmount: 30m,
                    outstandingAmount: 30m),
                HttpStatusCode.Created));

        var service =
            new LateFeeManagementWebService(
                apiClient);

        var result =
            await service.AddAdjustmentAsync(
                chargeId,
                new LateFeeAdjustmentRequest
                {
                    AdjustmentType =
                        LateFeeAdjustmentType
                            .Increase,

                    Amount =
                        10m,

                    Reason =
                        "Recargo administrativo autorizado"
                });

        Assert.NotNull(
            result);

        Assert.Equal(
            10m,
            result.IncreaseAmount);

        Assert.Equal(
            30m,
            result.OutstandingAmount);

        var request =
            Assert.Single(
                apiClient.Requests);

        AssertAdjustmentRequest(
            request,
            LateFeeAdjustmentType.Increase,
            10m,
            "Recargo administrativo autorizado");
    }

    [Fact]
    public async Task AddAdjustmentAsync_NotFound_ReturnsNull()
    {
        var apiClient =
            new TestSanesApiClient();

        apiClient.EnqueueResponse(
            new HttpResponseMessage(
                HttpStatusCode.NotFound));

        var service =
            new LateFeeManagementWebService(
                apiClient);

        var result =
            await service.AddAdjustmentAsync(
                Guid.NewGuid(),
                new LateFeeAdjustmentRequest
                {
                    AdjustmentType =
                        LateFeeAdjustmentType
                            .Waiver,

                    Amount =
                        5m,

                    Reason =
                        "Prueba"
                });

        Assert.Null(
            result);
    }

    [Fact]
    public async Task AddAdjustmentAsync_EmptyChargeId_ThrowsBeforeRequest()
    {
        var apiClient =
            new TestSanesApiClient();

        var service =
            new LateFeeManagementWebService(
                apiClient);

        var exception =
            await Assert.ThrowsAsync<
                ArgumentException>(
                    () =>
                        service.AddAdjustmentAsync(
                            Guid.Empty,
                            new LateFeeAdjustmentRequest
                            {
                                AdjustmentType =
                                    LateFeeAdjustmentType
                                        .Waiver,

                                Amount =
                                    5m,

                                Reason =
                                    "Prueba"
                            }));

        Assert.Contains(
            "cargo",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        Assert.Empty(
            apiClient.Requests);
    }

    [Fact]
    public async Task AddAdjustmentAsync_GreaterThanOutstanding_TranslatesMessage()
    {
        var apiClient =
            new TestSanesApiClient();

        apiClient.EnqueueResponse(
            JsonResponse(
                new
                {
                    message =
                        "Adjustment amount cannot exceed the outstanding late fee balance of 20.00."
                },
                HttpStatusCode.BadRequest));

        var service =
            new LateFeeManagementWebService(
                apiClient);

        var exception =
            await Assert.ThrowsAsync<
                ArgumentException>(
                    () =>
                        service.AddAdjustmentAsync(
                            Guid.NewGuid(),
                            new LateFeeAdjustmentRequest
                            {
                                AdjustmentType =
                                    LateFeeAdjustmentType
                                        .Decrease,

                                Amount =
                                    21m,

                                Reason =
                                    "Monto superior"
                            }));

        Assert.Equal(
            "El monto del ajuste no puede superar el saldo pendiente de mora de 20.00.",
            exception.Message);
    }

    [Fact]
    public async Task AddAdjustmentAsync_NoOutstandingBalance_TranslatesMessage()
    {
        var apiClient =
            new TestSanesApiClient();

        apiClient.EnqueueResponse(
            JsonResponse(
                new
                {
                    message =
                        "This late fee charge has no outstanding balance to reduce or waive."
                },
                HttpStatusCode.BadRequest));

        var service =
            new LateFeeManagementWebService(
                apiClient);

        var exception =
            await Assert.ThrowsAsync<
                ArgumentException>(
                    () =>
                        service.AddAdjustmentAsync(
                            Guid.NewGuid(),
                            new LateFeeAdjustmentRequest
                            {
                                AdjustmentType =
                                    LateFeeAdjustmentType
                                        .Waiver,

                                Amount =
                                    5m,

                                Reason =
                                    "Prueba"
                            }));

        Assert.Equal(
            "Este cargo de mora no tiene saldo pendiente que pueda disminuirse o perdonarse.",
            exception.Message);
    }

    [Fact]
    public async Task AddAdjustmentAsync_MissingReason_TranslatesMessage()
    {
        var apiClient =
            new TestSanesApiClient();

        apiClient.EnqueueResponse(
            JsonResponse(
                new
                {
                    message =
                        "Adjustment reason is required."
                },
                HttpStatusCode.BadRequest));

        var service =
            new LateFeeManagementWebService(
                apiClient);

        var exception =
            await Assert.ThrowsAsync<
                ArgumentException>(
                    () =>
                        service.AddAdjustmentAsync(
                            Guid.NewGuid(),
                            new LateFeeAdjustmentRequest
                            {
                                AdjustmentType =
                                    LateFeeAdjustmentType
                                        .Waiver,

                                Amount =
                                    5m,

                                Reason =
                                    string.Empty
                            }));

        Assert.Equal(
            "Debes indicar el motivo del ajuste.",
            exception.Message);
    }

    [Fact]
    public void FormModel_ForCharge_UsesOutstandingAsDefaultWaiver()
    {
        var chargeId =
            Guid.NewGuid();

        var model =
            LateFeeAdjustmentFormModel
                .ForCharge(
                    chargeId,
                    20m);

        Assert.Equal(
            chargeId,
            model.ChargeId);

        Assert.Equal(
            LateFeeAdjustmentType.Waiver,
            model.AdjustmentType);

        Assert.Equal(
            20m,
            model.Amount);

        Assert.Equal(
            string.Empty,
            model.Reason);
    }

    [Fact]
    public void FormModel_ToRequest_MapsAndTrimsValues()
    {
        var model =
            new LateFeeAdjustmentFormModel
            {
                ChargeId =
                    Guid.NewGuid(),

                AdjustmentType =
                    LateFeeAdjustmentType
                        .Decrease,

                Amount =
                    7.50m,

                Reason =
                    "  Corrección autorizada por administración  "
            };

        var request =
            model.ToRequest();

        Assert.Equal(
            LateFeeAdjustmentType.Decrease,
            request.AdjustmentType);

        Assert.Equal(
            7.50m,
            request.Amount);

        Assert.Equal(
            "Corrección autorizada por administración",
            request.Reason);
    }

    private static LateFeeChargeResponse
        CreateChargeResponse(
            Guid chargeId,
            Guid loanId,
            decimal increaseAmount = 0m,
            decimal decreaseAmount = 0m,
            decimal waivedAmount = 0m,
            decimal adjustedAmount = 20m,
            decimal outstandingAmount = 20m)
    {
        return new LateFeeChargeResponse
        {
            Id =
                chargeId,

            LoanId =
                loanId,

            InstallmentNumber =
                1,

            InstallmentDueDate =
                DateTime.UtcNow.Date
                    .AddDays(-1),

            EffectiveDate =
                DateTime.UtcNow.Date,

            CalculationType =
                LateFeeCalculationType
                    .FixedAmountPerInstallment,

            OriginalAmount =
                20m,

            IncreaseAmount =
                increaseAmount,

            DecreaseAmount =
                decreaseAmount,

            WaivedAmount =
                waivedAmount,

            AdjustedAmount =
                adjustedAmount,

            PaidAmount =
                0m,

            OutstandingAmount =
                outstandingAmount,

            CreatedAt =
                DateTime.UtcNow
        };
    }

    private static void AssertAdjustmentRequest(
        TestSanesApiClient.RecordedRequest request,
        LateFeeAdjustmentType expectedType,
        decimal expectedAmount,
        string expectedReason)
    {
        Assert.Equal(
            HttpMethod.Post,
            request.Method);

        AssertNoTenantOrUser(
            request.Uri);

        Assert.NotNull(
            request.Body);

        using var json =
            JsonDocument.Parse(
                request.Body!);

        var root =
            json.RootElement;

        Assert.Equal(
            (int)expectedType,
            root.GetProperty(
                    "adjustmentType")
                .GetInt32());

        Assert.Equal(
            expectedAmount,
            root.GetProperty(
                    "amount")
                .GetDecimal());

        Assert.Equal(
            expectedReason,
            root.GetProperty(
                    "reason")
                .GetString());

        Assert.False(
            root.TryGetProperty(
                "tenantId",
                out _));

        Assert.False(
            root.TryGetProperty(
                "appUserId",
                out _));
    }

    private static void AssertNoTenantOrUser(
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
    }

    private static HttpResponseMessage JsonResponse<T>(
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