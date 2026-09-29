using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Sanes.Application.Loans.DTOs;
using Sanes.Domain.Enums;
using Sanes.Web.EarlySettlements;

namespace Sanes.Web.Tests;

public class EarlySettlementsWebServiceTests
{
    [Fact]
    public async Task QuoteAsync_UsesExpectedEndpointAndBody()
    {
        var apiClient =
            new TestSanesApiClient();

        var loanId =
            Guid.NewGuid();

        apiClient.EnqueueResponse(
            JsonResponse(
                new EarlySettlementQuoteResponse
                {
                    LoanId = loanId,
                    IsEligible = true,
                    MinimumRequiredInstallments = 6,
                    CompletedInstallments = 6,
                    ContractualBalance = 700m,
                    LateFeeBalance = 20m,
                    TotalOutstanding = 720m,
                    DiscountType =
                        EarlySettlementDiscountType
                            .InstallmentWaiver,
                    DiscountValue = 2m,
                    DiscountAmount = 200m,
                    SettlementAmount = 520m,
                    QuotedAt = DateTime.UtcNow
                }));

        var service =
            new EarlySettlementsWebService(
                apiClient);

        var result =
            await service.QuoteAsync(
                loanId,
                new EarlySettlementQuoteRequest
                {
                    DiscountType =
                        EarlySettlementDiscountType
                            .InstallmentWaiver,

                    DiscountValue =
                        2m
                });

        Assert.NotNull(
            result);

        Assert.Equal(
            520m,
            result.SettlementAmount);

        var request =
            Assert.Single(
                apiClient.Requests);

        Assert.Equal(
            HttpMethod.Post,
            request.Method);

        Assert.Equal(
            $"api/loans/{loanId:D}/early-settlement/quote",
            request.Uri);

        AssertNoTenantParameter(
            request.Uri);

        Assert.NotNull(
            request.Body);

        using var json =
            JsonDocument.Parse(
                request.Body!);

        var root =
            json.RootElement;

        Assert.Equal(
            (int)EarlySettlementDiscountType
                .InstallmentWaiver,
            root.GetProperty(
                    "discountType")
                .GetInt32());

        Assert.Equal(
            2m,
            root.GetProperty(
                    "discountValue")
                .GetDecimal());

        Assert.False(
            root.TryGetProperty(
                "tenantId",
                out _));

        Assert.False(
            root.TryGetProperty(
                "appUserId",
                out _));
    }

    [Fact]
    public async Task QuoteAsync_NotFound_ReturnsNull()
    {
        var apiClient =
            new TestSanesApiClient();

        apiClient.EnqueueResponse(
            new HttpResponseMessage(
                HttpStatusCode.NotFound));

        var service =
            new EarlySettlementsWebService(
                apiClient);

        var result =
            await service.QuoteAsync(
                Guid.NewGuid(),
                new EarlySettlementQuoteRequest
                {
                    DiscountType =
                        EarlySettlementDiscountType
                            .InstallmentWaiver,

                    DiscountValue =
                        1m
                });

        Assert.Null(
            result);
    }

    [Fact]
    public async Task QuoteAsync_Ineligible_TranslatesMessage()
    {
        var apiClient =
            new TestSanesApiClient();

        apiClient.EnqueueResponse(
            JsonResponse(
                new
                {
                    message =
                        "At least 6 completed installments are required for early settlement. Current completed installments: 5."
                },
                HttpStatusCode.BadRequest));

        var service =
            new EarlySettlementsWebService(
                apiClient);

        var exception =
            await Assert.ThrowsAsync<
                ArgumentException>(
                    () =>
                        service.QuoteAsync(
                            Guid.NewGuid(),
                            new EarlySettlementQuoteRequest
                            {
                                DiscountType =
                                    EarlySettlementDiscountType
                                        .InstallmentWaiver,

                                DiscountValue =
                                    1m
                            }));

        Assert.Equal(
            "Se requieren al menos 6 cuotas completadas para realizar una liquidación anticipada.",
            exception.Message);
    }

    [Fact]
    public async Task QuoteAsync_EmptyLoanId_ThrowsBeforeRequest()
    {
        var apiClient =
            new TestSanesApiClient();

        var service =
            new EarlySettlementsWebService(
                apiClient);

        await Assert.ThrowsAsync<
            ArgumentException>(
                () =>
                    service.QuoteAsync(
                        Guid.Empty,
                        new EarlySettlementQuoteRequest
                        {
                            DiscountType =
                                EarlySettlementDiscountType
                                    .InstallmentWaiver,

                            DiscountValue =
                                1m
                        }));

        Assert.Empty(
            apiClient.Requests);
    }

    [Fact]
    public async Task ExecuteAsync_UsesExpectedEndpointAndBody()
    {
        var apiClient =
            new TestSanesApiClient();

        var loanId =
            Guid.NewGuid();

        var settlementId =
            Guid.NewGuid();

        apiClient.EnqueueResponse(
            JsonResponse(
                new EarlySettlementResponse
                {
                    Id = settlementId,
                    LoanId = loanId,
                    PaymentId = Guid.NewGuid(),
                    LoanBalanceAdjustmentId =
                        Guid.NewGuid(),
                    CompletedInstallments = 6,
                    ContractualBalanceBefore = 700m,
                    LateFeeBalanceBefore = 20m,
                    TotalOutstandingBefore = 720m,
                    DiscountType =
                        EarlySettlementDiscountType
                            .PercentageDiscount,
                    DiscountValue = 10m,
                    DiscountAmount = 70m,
                    SettlementAmount = 650m,
                    AppliedToLateFees = 20m,
                    AppliedToLoan = 630m,
                    ContractualBalanceAfter = 0m,
                    LateFeeBalanceAfter = 0m,
                    TotalOutstandingAfter = 0m,
                    CreatedAt = DateTime.UtcNow
                }));

        var service =
            new EarlySettlementsWebService(
                apiClient);

        var result =
            await service.ExecuteAsync(
                loanId,
                new EarlySettlementExecuteRequest
                {
                    DiscountType =
                        EarlySettlementDiscountType
                            .PercentageDiscount,

                    DiscountValue =
                        10m,

                    ExpectedSettlementAmount =
                        650m,

                    Reason =
                        "Autorizado por administración"
                });

        Assert.NotNull(
            result);

        Assert.Equal(
            settlementId,
            result.Id);

        var request =
            Assert.Single(
                apiClient.Requests);

        Assert.Equal(
            HttpMethod.Post,
            request.Method);

        Assert.Equal(
            $"api/loans/{loanId:D}/early-settlement",
            request.Uri);

        AssertNoTenantParameter(
            request.Uri);

        Assert.NotNull(
            request.Body);

        using var json =
            JsonDocument.Parse(
                request.Body!);

        var root =
            json.RootElement;

        Assert.Equal(
            (int)EarlySettlementDiscountType
                .PercentageDiscount,
            root.GetProperty(
                    "discountType")
                .GetInt32());

        Assert.Equal(
            10m,
            root.GetProperty(
                    "discountValue")
                .GetDecimal());

        Assert.Equal(
            650m,
            root.GetProperty(
                    "expectedSettlementAmount")
                .GetDecimal());

        Assert.Equal(
            "Autorizado por administración",
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

    [Fact]
    public async Task ExecuteAsync_StaleQuote_TranslatesMessage()
    {
        var apiClient =
            new TestSanesApiClient();

        apiClient.EnqueueResponse(
            JsonResponse(
                new
                {
                    message =
                        "The settlement amount has changed. Expected 500.00, current amount is 450.00. Request a new quote before continuing."
                },
                HttpStatusCode.BadRequest));

        var service =
            new EarlySettlementsWebService(
                apiClient);

        var exception =
            await Assert.ThrowsAsync<
                ArgumentException>(
                    () =>
                        service.ExecuteAsync(
                            Guid.NewGuid(),
                            new EarlySettlementExecuteRequest
                            {
                                DiscountType =
                                    EarlySettlementDiscountType
                                        .InstallmentWaiver,

                                DiscountValue =
                                    2m,

                                ExpectedSettlementAmount =
                                    500m,

                                Reason =
                                    "Cotización anterior"
                            }));

        Assert.Equal(
            "El monto de la liquidación cambió desde la última cotización. Solicita una nueva cotización antes de continuar.",
            exception.Message);
    }

    [Fact]
    public async Task ExecuteAsync_NotFound_ReturnsNull()
    {
        var apiClient =
            new TestSanesApiClient();

        apiClient.EnqueueResponse(
            new HttpResponseMessage(
                HttpStatusCode.NotFound));

        var service =
            new EarlySettlementsWebService(
                apiClient);

        var result =
            await service.ExecuteAsync(
                Guid.NewGuid(),
                new EarlySettlementExecuteRequest
                {
                    DiscountType =
                        EarlySettlementDiscountType
                            .InstallmentWaiver,

                    DiscountValue =
                        1m,

                    ExpectedSettlementAmount =
                        500m,

                    Reason =
                        "Prueba"
                });

        Assert.Null(
            result);
    }

    [Fact]
    public async Task GetByLoanAsync_UsesExpectedEndpoint()
    {
        var apiClient =
            new TestSanesApiClient();

        var loanId =
            Guid.NewGuid();

        apiClient.EnqueueResponse(
            JsonResponse(
                new EarlySettlementResponse
                {
                    Id = Guid.NewGuid(),
                    LoanId = loanId,
                    LoanBalanceAdjustmentId =
                        Guid.NewGuid(),
                    CompletedInstallments = 6,
                    DiscountType =
                        EarlySettlementDiscountType
                            .InstallmentWaiver,
                    DiscountValue = 2m,
                    DiscountAmount = 200m,
                    SettlementAmount = 500m,
                    CreatedAt = DateTime.UtcNow
                }));

        var service =
            new EarlySettlementsWebService(
                apiClient);

        var result =
            await service.GetByLoanAsync(
                loanId);

        Assert.NotNull(
            result);

        Assert.Equal(
            loanId,
            result.LoanId);

        var request =
            Assert.Single(
                apiClient.Requests);

        Assert.Equal(
            HttpMethod.Get,
            request.Method);

        Assert.Equal(
            $"api/loans/{loanId:D}/early-settlement",
            request.Uri);

        AssertNoTenantParameter(
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
            new EarlySettlementsWebService(
                apiClient);

        var result =
            await service.GetByLoanAsync(
                Guid.NewGuid());

        Assert.Null(
            result);
    }

    [Fact]
    public void FormModel_ToQuoteRequest_MapsSelectedDiscount()
    {
        var model =
            new EarlySettlementFormModel
            {
                LoanId =
                    Guid.NewGuid(),

                DiscountType =
                    EarlySettlementDiscountType
                        .PercentageDiscount,

                DiscountValue =
                    15m
            };

        var request =
            model.ToQuoteRequest();

        Assert.Equal(
            EarlySettlementDiscountType
                .PercentageDiscount,
            request.DiscountType);

        Assert.Equal(
            15m,
            request.DiscountValue);
    }

    [Fact]
    public void FormModel_ToExecuteRequest_UsesQuotedAmountAndTrimsReason()
    {
        var loanId =
            Guid.NewGuid();

        var model =
            new EarlySettlementFormModel
            {
                LoanId =
                    loanId,

                DiscountType =
                    EarlySettlementDiscountType
                        .InstallmentWaiver,

                DiscountValue =
                    2m,

                Reason =
                    "  Cliente solicitó cierre anticipado  "
            };

        var quote =
            new EarlySettlementQuoteResponse
            {
                LoanId =
                    loanId,

                DiscountType =
                    EarlySettlementDiscountType
                        .InstallmentWaiver,

                DiscountValue =
                    2m,

                DiscountAmount =
                    200m,

                SettlementAmount =
                    500m
            };

        var request =
            model.ToExecuteRequest(
                quote);

        Assert.Equal(
            EarlySettlementDiscountType
                .InstallmentWaiver,
            request.DiscountType);

        Assert.Equal(
            2m,
            request.DiscountValue);

        Assert.Equal(
            500m,
            request.ExpectedSettlementAmount);

        Assert.Equal(
            "Cliente solicitó cierre anticipado",
            request.Reason);
    }

    [Fact]
    public void FormModel_ForLoan_UsesSafeDefaults()
    {
        var loanId =
            Guid.NewGuid();

        var model =
            EarlySettlementFormModel
                .ForLoan(
                    loanId);

        Assert.Equal(
            loanId,
            model.LoanId);

        Assert.Equal(
            EarlySettlementDiscountType
                .InstallmentWaiver,
            model.DiscountType);

        Assert.Equal(
            1m,
            model.DiscountValue);

        Assert.Null(
            model.Reason);
    }

    private static void AssertNoTenantParameter(
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