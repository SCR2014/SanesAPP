using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Sanes.Application.Clients.DTOs;
using Sanes.Application.Investors.DTOs;
using Sanes.Application.Loans.DTOs;
using Sanes.Application.Tenants.DTOs;
using Sanes.Domain.Enums;
using Sanes.Web.Loans;

namespace Sanes.Web.Tests;

public class LoansWebServiceTests
{
    [Fact]
    public async Task GetAllAsync_UsesLoansEndpoint()
    {
        var apiClient =
            new TestSanesApiClient();

        apiClient.EnqueueResponse(
            JsonResponse(
                new List<LoanResponse>
                {
                    new()
                    {
                        Id =
                            Guid.NewGuid(),

                        ClientName =
                            "Carlos Rodriguez",

                        InvestorName =
                            "Juan Perez",

                        PrincipalAmount =
                            1000m,

                        InstallmentAmount =
                            100m,

                        TotalInstallments =
                            13,

                        TotalAmount =
                            1300m,

                        PaymentFrequency =
                            PaymentFrequency.Weekly,

                        Status =
                            LoanStatus.Active
                    }
                }));

        var service =
            new LoansWebService(
                apiClient);

        var result =
            await service.GetAllAsync();

        var loan =
            Assert.Single(
                result);

        Assert.Equal(
            "Carlos Rodriguez",
            loan.ClientName);

        Assert.Equal(
            "Juan Perez",
            loan.InvestorName);

        AssertRequest(
            Assert.Single(
                apiClient.Requests),
            HttpMethod.Get,
            "api/loans");
    }

    [Fact]
    public async Task GetByIdAsync_UsesExpectedEndpoint()
    {
        var apiClient =
            new TestSanesApiClient();

        var loanId =
            Guid.NewGuid();

        apiClient.EnqueueResponse(
            JsonResponse(
                new LoanResponse
                {
                    Id =
                        loanId,

                    ClientName =
                        "Cliente Detalle",

                    InvestorName =
                        "Investor Detalle",

                    PrincipalAmount =
                        500m,

                    InstallmentAmount =
                        50m,

                    TotalInstallments =
                        13,

                    TotalAmount =
                        650m,

                    PaymentFrequency =
                        PaymentFrequency.Weekly,

                    Status =
                        LoanStatus.Active
                }));

        var service =
            new LoansWebService(
                apiClient);

        var result =
            await service.GetByIdAsync(
                loanId);

        Assert.NotNull(
            result);

        Assert.Equal(
            loanId,
            result.Id);

        Assert.Equal(
            "Cliente Detalle",
            result.ClientName);

        AssertRequest(
            Assert.Single(
                apiClient.Requests),
            HttpMethod.Get,
            $"api/loans/{loanId:D}");
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNull()
    {
        var apiClient =
            new TestSanesApiClient();

        var loanId =
            Guid.NewGuid();

        apiClient.EnqueueResponse(
            new HttpResponseMessage(
                HttpStatusCode.NotFound));

        var service =
            new LoansWebService(
                apiClient);

        var result =
            await service.GetByIdAsync(
                loanId);

        Assert.Null(
            result);

        AssertRequest(
            Assert.Single(
                apiClient.Requests),
            HttpMethod.Get,
            $"api/loans/{loanId:D}");
    }

    [Fact]
    public async Task GetFinancialSummaryAsync_UsesExpectedEndpoint()
    {
        var apiClient =
            new TestSanesApiClient();

        var loanId =
            Guid.NewGuid();

        apiClient.EnqueueResponse(
            JsonResponse(
                new LoanFinancialSummaryResponse
                {
                    PrincipalAmount =
                        1000m,

                    TotalAmount =
                        1300m,

                    InterestAmount =
                        300m,

                    AmountPaid =
                        400m,

                    Balance =
                        900m,

                    InstallmentAmount =
                        100m,

                    TotalInstallments =
                        13,

                    CompletedInstallments =
                        4,

                    RemainingInstallments =
                        9,

                    CurrentInstallmentPaidAmount =
                        0m,

                    CurrentInstallmentRemainingAmount =
                        100m,

                    NextInstallmentAmountDue =
                        100m,

                    LateFeeBalance =
                        20m,

                    TotalOutstanding =
                        920m,

                    PercentagePaid =
                        30.77m,

                    PaymentFrequency =
                        PaymentFrequency.Weekly,

                    Status =
                        LoanStatus.Active
                }));

        var service =
            new LoansWebService(
                apiClient);

        var result =
            await service
                .GetFinancialSummaryAsync(
                    loanId);

        Assert.NotNull(
            result);

        Assert.Equal(
            900m,
            result.Balance);

        Assert.Equal(
            20m,
            result.LateFeeBalance);

        Assert.Equal(
            920m,
            result.TotalOutstanding);

        AssertRequest(
            Assert.Single(
                apiClient.Requests),
            HttpMethod.Get,
            $"api/loans/{loanId:D}/summary");
    }

    [Fact]
    public async Task GetFinancialSummaryAsync_WhenNotFound_ReturnsNull()
    {
        var apiClient =
            new TestSanesApiClient();

        var loanId =
            Guid.NewGuid();

        apiClient.EnqueueResponse(
            new HttpResponseMessage(
                HttpStatusCode.NotFound));

        var service =
            new LoansWebService(
                apiClient);

        var result =
            await service
                .GetFinancialSummaryAsync(
                    loanId);

        Assert.Null(
            result);

        AssertRequest(
            Assert.Single(
                apiClient.Requests),
            HttpMethod.Get,
            $"api/loans/{loanId:D}/summary");
    }

    [Fact]
    public async Task GetTenantAsync_UsesCurrentTenantEndpoint()
    {
        var apiClient =
            new TestSanesApiClient();

        apiClient.EnqueueResponse(
            JsonResponse(
                new TenantDto
                {
                    Id =
                        Guid.NewGuid(),

                    Name =
                        "Sanes Test",

                    CurrencyCode =
                        "DOP",

                    CurrencySymbol =
                        "RD$",

                    DefaultLateFeeEnabled =
                        true,

                    DefaultLateFeeAmount =
                        20m,

                    DefaultLateFeeGraceDays =
                        0,

                    GuaranteeRequiredFromAmount =
                        1000m,

                    IsActive =
                        true
                }));

        var service =
            new LoansWebService(
                apiClient);

        var tenant =
            await service.GetTenantAsync();

        Assert.Equal(
            "Sanes Test",
            tenant.Name);

        Assert.Equal(
            "DOP",
            tenant.CurrencyCode);

        Assert.Equal(
            1000m,
            tenant.GuaranteeRequiredFromAmount);

        AssertRequest(
            Assert.Single(
                apiClient.Requests),
            HttpMethod.Get,
            "api/tenants/me");
    }

    [Fact]
    public async Task GetClientsAsync_UsesClientsEndpoint()
    {
        var apiClient =
            new TestSanesApiClient();

        apiClient.EnqueueResponse(
            JsonResponse(
                new List<ClientResponse>
                {
                    new()
                    {
                        Id =
                            Guid.NewGuid(),

                        FirstName =
                            "Carlos",

                        LastName =
                            "Rodriguez",

                        Phone =
                            "8095551234",

                        IsActive =
                            true
                    }
                }));

        var service =
            new LoansWebService(
                apiClient);

        var clients =
            await service.GetClientsAsync();

        var client =
            Assert.Single(
                clients);

        Assert.Equal(
            "Carlos",
            client.FirstName);

        Assert.True(
            client.IsActive);

        AssertRequest(
            Assert.Single(
                apiClient.Requests),
            HttpMethod.Get,
            "api/clients");
    }

    [Fact]
    public async Task GetInvestorsAsync_UsesInvestorsEndpoint()
    {
        var apiClient =
            new TestSanesApiClient();

        apiClient.EnqueueResponse(
            JsonResponse(
                new List<InvestorResponse>
                {
                    new()
                    {
                        Id =
                            Guid.NewGuid(),

                        Name =
                            "Investor Test",

                        Phone =
                            "8095551000",

                        Identification =
                            "INV-001",

                        IsActive =
                            true
                    }
                }));

        var service =
            new LoansWebService(
                apiClient);

        var investors =
            await service.GetInvestorsAsync();

        var investor =
            Assert.Single(
                investors);

        Assert.Equal(
            "Investor Test",
            investor.Name);

        Assert.True(
            investor.IsActive);

        AssertRequest(
            Assert.Single(
                apiClient.Requests),
            HttpMethod.Get,
            "api/investors");
    }

    [Fact]
    public async Task CreateAsync_SendsExpectedRequest()
    {
        var apiClient =
            new TestSanesApiClient();

        var investorId =
            Guid.NewGuid();

        var clientId =
            Guid.NewGuid();

        var loanId =
            Guid.NewGuid();

        apiClient.EnqueueResponse(
            JsonResponse(
                new LoanResponse
                {
                    Id =
                        loanId,

                    InvestorId =
                        investorId,

                    InvestorName =
                        "Investor Test",

                    ClientId =
                        clientId,

                    ClientName =
                        "Cliente Test",

                    PrincipalAmount =
                        1000m,

                    InstallmentAmount =
                        100m,

                    TotalInstallments =
                        13,

                    TotalAmount =
                        1300m,

                    PaymentFrequency =
                        PaymentFrequency.Weekly,

                    Status =
                        LoanStatus.Active
                },
                HttpStatusCode.Created));

        var service =
            new LoansWebService(
                apiClient);

        var result =
            await service.CreateAsync(
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
                        new DateTime(
                            2026,
                            9,
                            28),

                    Notes =
                        "Préstamo desde Web"
                });

        Assert.Equal(
            loanId,
            result.Id);

        Assert.Equal(
            "Cliente Test",
            result.ClientName);

        var recorded =
            Assert.Single(
                apiClient.Requests);

        AssertRequest(
            recorded,
            HttpMethod.Post,
            "api/loans");

        Assert.NotNull(
            recorded.Body);

        using var document =
            JsonDocument.Parse(
                recorded.Body!);

        var root =
            document.RootElement;

        Assert.Equal(
            investorId,
            root.GetProperty(
                    "investorId")
                .GetGuid());

        Assert.Equal(
            clientId,
            root.GetProperty(
                    "clientId")
                .GetGuid());

        Assert.Equal(
            1000m,
            root.GetProperty(
                    "principalAmount")
                .GetDecimal());

        Assert.Equal(
            100m,
            root.GetProperty(
                    "installmentAmount")
                .GetDecimal());

        Assert.Equal(
            13,
            root.GetProperty(
                    "totalInstallments")
                .GetInt32());

        Assert.False(
            root.TryGetProperty(
                "tenantId",
                out _));
    }

    [Fact]
    public async Task CreateAsync_WithGuarantee_SendsGuaranteeWithoutTenantId()
    {
        var apiClient =
            new TestSanesApiClient();

        var investorId =
            Guid.NewGuid();

        var clientId =
            Guid.NewGuid();

        apiClient.EnqueueResponse(
            JsonResponse(
                new LoanResponse
                {
                    Id =
                        Guid.NewGuid(),

                    InvestorId =
                        investorId,

                    InvestorName =
                        "Investor Test",

                    ClientId =
                        clientId,

                    ClientName =
                        "Cliente Garantia",

                    PrincipalAmount =
                        2000m,

                    InstallmentAmount =
                        200m,

                    TotalInstallments =
                        13,

                    TotalAmount =
                        2600m,

                    PaymentFrequency =
                        PaymentFrequency.Weekly,

                    Status =
                        LoanStatus.Active
                },
                HttpStatusCode.Created));

        var service =
            new LoansWebService(
                apiClient);

        await service.CreateAsync(
            new CreateLoanRequest
            {
                InvestorId =
                    investorId,

                ClientId =
                    clientId,

                PrincipalAmount =
                    2000m,

                InstallmentAmount =
                    200m,

                TotalInstallments =
                    13,

                PaymentFrequency =
                    PaymentFrequency.Weekly,

                StartDate =
                    new DateTime(
                        2026,
                        9,
                        28),

                Guarantee =
                    new CreateLoanGuaranteeRequest
                    {
                        Type =
                            default,

                        Reference =
                            "GAR-001",

                        Description =
                            "Garantía de prueba"
                    }
            });

        var recorded =
            Assert.Single(
                apiClient.Requests);

        Assert.NotNull(
            recorded.Body);

        using var document =
            JsonDocument.Parse(
                recorded.Body!);

        var root =
            document.RootElement;

        Assert.True(
            root.TryGetProperty(
                "guarantee",
                out var guarantee));

        Assert.Equal(
            "GAR-001",
            guarantee
                .GetProperty(
                    "reference")
                .GetString());

        Assert.False(
            root.TryGetProperty(
                "tenantId",
                out _));

        Assert.False(
            guarantee.TryGetProperty(
                "tenantId",
                out _));
    }

    [Fact]
    public async Task CreateAsync_WhenBadRequest_ThrowsApiMessage()
    {
        var apiClient =
            new TestSanesApiClient();

        apiClient.EnqueueResponse(
            JsonResponse(
                new
                {
                    message =
                        "A guarantee is required for this loan."
                },
                HttpStatusCode.BadRequest));

        var service =
            new LoansWebService(
                apiClient);

        var exception =
            await Assert.ThrowsAsync<
                ArgumentException>(
                () =>
                    service.CreateAsync(
                        new CreateLoanRequest
                        {
                            InvestorId =
                                Guid.NewGuid(),

                            ClientId =
                                Guid.NewGuid(),

                            PrincipalAmount =
                                2000m,

                            InstallmentAmount =
                                200m,

                            TotalInstallments =
                                13,

                            PaymentFrequency =
                                PaymentFrequency.Weekly,

                            StartDate =
                                DateTime.UtcNow
                        }));

        Assert.Equal(
            "A guarantee is required for this loan.",
            exception.Message);
    }

    [Fact]
    public async Task UpdateAsync_SendsExpectedRequest()
    {
        var apiClient =
            new TestSanesApiClient();

        var loanId =
            Guid.NewGuid();

        apiClient.EnqueueResponse(
            JsonResponse(
                new LoanResponse
                {
                    Id =
                        loanId,

                    ClientName =
                        "Cliente Actualizado",

                    InvestorName =
                        "Investor Test",

                    PrincipalAmount =
                        1200m,

                    InstallmentAmount =
                        120m,

                    TotalInstallments =
                        13,

                    TotalAmount =
                        1560m,

                    PaymentFrequency =
                        PaymentFrequency.Weekly,

                    Status =
                        LoanStatus.Active
                }));

        var service =
            new LoansWebService(
                apiClient);

        var result =
            await service.UpdateAsync(
                loanId,
                new UpdateLoanRequest
                {
                    PrincipalAmount =
                        1200m,

                    InstallmentAmount =
                        120m,

                    TotalInstallments =
                        13,

                    PaymentFrequency =
                        PaymentFrequency.Weekly,

                    StartDate =
                        new DateTime(
                            2026,
                            9,
                            28),

                    Notes =
                        "Actualizado desde Web"
                });

        Assert.NotNull(
            result);

        Assert.Equal(
            1200m,
            result.PrincipalAmount);

        var recorded =
            Assert.Single(
                apiClient.Requests);

        AssertRequest(
            recorded,
            HttpMethod.Put,
            $"api/loans/{loanId:D}");

        Assert.NotNull(
            recorded.Body);

        using var document =
            JsonDocument.Parse(
                recorded.Body!);

        var root =
            document.RootElement;

        Assert.Equal(
            1200m,
            root.GetProperty(
                    "principalAmount")
                .GetDecimal());

        Assert.Equal(
            120m,
            root.GetProperty(
                    "installmentAmount")
                .GetDecimal());

        Assert.False(
            root.TryGetProperty(
                "tenantId",
                out _));

        Assert.False(
            root.TryGetProperty(
                "clientId",
                out _));

        Assert.False(
            root.TryGetProperty(
                "investorId",
                out _));
    }

    [Fact]
    public async Task UpdateAsync_WhenNotFound_ReturnsNull()
    {
        var apiClient =
            new TestSanesApiClient();

        var loanId =
            Guid.NewGuid();

        apiClient.EnqueueResponse(
            new HttpResponseMessage(
                HttpStatusCode.NotFound));

        var service =
            new LoansWebService(
                apiClient);

        var result =
            await service.UpdateAsync(
                loanId,
                new UpdateLoanRequest
                {
                    PrincipalAmount =
                        1000m,

                    InstallmentAmount =
                        100m,

                    TotalInstallments =
                        13,

                    PaymentFrequency =
                        PaymentFrequency.Weekly,

                    StartDate =
                        DateTime.UtcNow
                });

        Assert.Null(
            result);

        AssertRequest(
            Assert.Single(
                apiClient.Requests),
            HttpMethod.Put,
            $"api/loans/{loanId:D}");
    }

    [Fact]
    public async Task UpdateAsync_WhenBadRequest_ThrowsApiMessage()
    {
        var apiClient =
            new TestSanesApiClient();

        var loanId =
            Guid.NewGuid();

        apiClient.EnqueueResponse(
            JsonResponse(
                new
                {
                    message =
                        "Only active loans can be updated."
                },
                HttpStatusCode.BadRequest));

        var service =
            new LoansWebService(
                apiClient);

        var exception =
            await Assert.ThrowsAsync<
                ArgumentException>(
                () =>
                    service.UpdateAsync(
                        loanId,
                        new UpdateLoanRequest
                        {
                            PrincipalAmount =
                                1000m,

                            InstallmentAmount =
                                100m,

                            TotalInstallments =
                                13,

                            PaymentFrequency =
                                PaymentFrequency.Weekly,

                            StartDate =
                                DateTime.UtcNow
                        }));

        Assert.Equal(
            "Only active loans can be updated.",
            exception.Message);
    }

    [Fact]
    public async Task CancelAsync_UsesExpectedEndpoint()
    {
        var apiClient =
            new TestSanesApiClient();

        var loanId =
            Guid.NewGuid();

        apiClient.EnqueueResponse(
            new HttpResponseMessage(
                HttpStatusCode.NoContent));

        var service =
            new LoansWebService(
                apiClient);

        var result =
            await service.CancelAsync(
                loanId);

        Assert.True(
            result);

        AssertRequest(
            Assert.Single(
                apiClient.Requests),
            HttpMethod.Patch,
            $"api/loans/{loanId:D}/cancel");
    }

    [Fact]
    public async Task CancelAsync_WhenNotFound_ReturnsFalse()
    {
        var apiClient =
            new TestSanesApiClient();

        var loanId =
            Guid.NewGuid();

        apiClient.EnqueueResponse(
            new HttpResponseMessage(
                HttpStatusCode.NotFound));

        var service =
            new LoansWebService(
                apiClient);

        var result =
            await service.CancelAsync(
                loanId);

        Assert.False(
            result);

        AssertRequest(
            Assert.Single(
                apiClient.Requests),
            HttpMethod.Patch,
            $"api/loans/{loanId:D}/cancel");
    }

    [Fact]
    public async Task CancelAsync_WhenBadRequest_ThrowsApiMessage()
    {
        var apiClient =
            new TestSanesApiClient();

        var loanId =
            Guid.NewGuid();

        apiClient.EnqueueResponse(
            JsonResponse(
                new
                {
                    message =
                        "Only active loans can be cancelled."
                },
                HttpStatusCode.BadRequest));

        var service =
            new LoansWebService(
                apiClient);

        var exception =
            await Assert.ThrowsAsync<
                ArgumentException>(
                () =>
                    service.CancelAsync(
                        loanId));

        Assert.Equal(
            "Only active loans can be cancelled.",
            exception.Message);
    }

    private static void AssertRequest(
        TestSanesApiClient.RecordedRequest request,
        HttpMethod expectedMethod,
        string expectedUri)
    {
        Assert.Equal(
            expectedMethod,
            request.Method);

        Assert.Equal(
            expectedUri,
            request.Uri);

        Assert.False(
            request.Uri.Contains(
                "tenantId",
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