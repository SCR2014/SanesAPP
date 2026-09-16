using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Sanes.Api.IntegrationTests.Helpers;
using Sanes.Application.Authentication.DTOs;
using Sanes.Application.Clients.DTOs;
using Sanes.Application.Investors.DTOs;
using Sanes.Application.Loans.DTOs;
using Sanes.Application.Tenants.DTOs;
using Sanes.Domain.Enums;

namespace Sanes.Api.IntegrationTests.LoanGuarantees;

public class LoanGuaranteesTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public LoanGuaranteesTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ============================================================
    // TENANT POLICY
    // ============================================================

    [Fact]
    public async Task UpdateTenant_WithZeroGuaranteeThreshold_ReturnsBadRequest()
    {
        var context =
            await CreateContextAsync();

        var response =
            await UpdateGuaranteeThresholdAsync(
                context,
                0m);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    // ============================================================
    // LOAN CREATION
    // ============================================================

    [Fact]
    public async Task CreateLoan_WithoutThreshold_DoesNotRequireGuarantee()
    {
        var setup =
            await CreateBaseSetupAsync();

        var loan =
            await CreateLoanAsync(
                setup.Context.Client,
                setup.InvestorId,
                setup.ClientId,
                principalAmount: 1000m);

        Assert.False(
            loan.GuaranteeRequired);

        Assert.Null(
            loan.GuaranteeThresholdAtCreation);

        Assert.Null(
            loan.Guarantee);
    }

    [Fact]
    public async Task CreateLoan_BelowThreshold_WithoutGuarantee_Succeeds()
    {
        var setup =
            await CreateBaseSetupAsync();

        await ConfigureGuaranteeThresholdAsync(
            setup.Context,
            1000m);

        var loan =
            await CreateLoanAsync(
                setup.Context.Client,
                setup.InvestorId,
                setup.ClientId,
                principalAmount: 900m);

        Assert.False(
            loan.GuaranteeRequired);

        Assert.Equal(
            1000m,
            loan.GuaranteeThresholdAtCreation!.Value);

        Assert.Null(
            loan.Guarantee);
    }

    [Fact]
    public async Task CreateLoan_AtThreshold_WithoutGuarantee_ReturnsBadRequest()
    {
        var setup =
            await CreateBaseSetupAsync();

        await ConfigureGuaranteeThresholdAsync(
            setup.Context,
            1000m);

        var response =
            await CreateLoanRawAsync(
                setup.Context.Client,
                setup.InvestorId,
                setup.ClientId,
                principalAmount: 1000m);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task CreateLoan_AboveThreshold_WithGuarantee_PersistsSnapshot()
    {
        var setup =
            await CreateBaseSetupAsync();

        await ConfigureGuaranteeThresholdAsync(
            setup.Context,
            1000m);

        var loan =
            await CreateLoanAsync(
                setup.Context.Client,
                setup.InvestorId,
                setup.ClientId,
                principalAmount: 1200m,
                guarantee:
                    new CreateLoanGuaranteeRequest
                    {
                        Type =
                            LoanGuaranteeType.Collateral,

                        Reference =
                            "VEH-001",

                        Description =
                            "Vehicle used as collateral"
                    });

        Assert.True(
            loan.GuaranteeRequired);

        Assert.Equal(
            1000m,
            loan.GuaranteeThresholdAtCreation!.Value);

        Assert.NotNull(
            loan.Guarantee);

        Assert.Equal(
            LoanGuaranteeType.Collateral,
            loan.Guarantee.Type);

        Assert.Equal(
            "VEH-001",
            loan.Guarantee.Reference);
    }

    [Fact]
    public async Task CreateLoan_BelowThreshold_WithVoluntaryGuarantee_PersistsGuarantee()
    {
        var setup =
            await CreateBaseSetupAsync();

        await ConfigureGuaranteeThresholdAsync(
            setup.Context,
            1000m);

        var loan =
            await CreateLoanAsync(
                setup.Context.Client,
                setup.InvestorId,
                setup.ClientId,
                principalAmount: 900m,
                guarantee:
                    new CreateLoanGuaranteeRequest
                    {
                        Type =
                            LoanGuaranteeType
                                .IdentificationDocument,

                        Reference =
                            "ID-001",

                        Description =
                            "Voluntary guarantee"
                    });

        Assert.False(
            loan.GuaranteeRequired);

        Assert.NotNull(
            loan.Guarantee);

        Assert.Equal(
            "ID-001",
            loan.Guarantee.Reference);
    }

    [Fact]
    public async Task TenantThresholdChange_DoesNotModifyExistingLoanSnapshot()
    {
        var setup =
            await CreateBaseSetupAsync();

        await ConfigureGuaranteeThresholdAsync(
            setup.Context,
            1000m);

        var loan =
            await CreateLoanAsync(
                setup.Context.Client,
                setup.InvestorId,
                setup.ClientId,
                principalAmount: 1200m,
                guarantee:
                    new CreateLoanGuaranteeRequest
                    {
                        Type =
                            LoanGuaranteeType.Collateral,

                        Reference =
                            "SNAPSHOT-001"
                    });

        Assert.Equal(
            1000m,
            loan.GuaranteeThresholdAtCreation!.Value);

        await ConfigureGuaranteeThresholdAsync(
            setup.Context,
            500m);

        var response =
            await setup.Context.Client.GetAsync(
                $"/api/loans/{loan.Id}");

        response.EnsureSuccessStatusCode();

        var stored =
            await response.Content
                .ReadFromJsonAsync<LoanResponse>();

        Assert.NotNull(
            stored);

        Assert.Equal(
            1000m,
            stored.GuaranteeThresholdAtCreation!.Value);

        Assert.True(
            stored.GuaranteeRequired);
    }

    // ============================================================
    // GUARANTEE API
    // ============================================================

    [Fact]
    public async Task GetGuarantee_WhenMissing_ReturnsNotFound()
    {
        var setup =
            await CreateBaseSetupAsync();

        var loan =
            await CreateLoanAsync(
                setup.Context.Client,
                setup.InvestorId,
                setup.ClientId,
                900m);

        var response =
            await setup.Context.Client.GetAsync(
                $"/api/loans/{loan.Id}/guarantee");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task PostGuarantee_RegistersGuarantee()
    {
        var setup =
            await CreateBaseSetupAsync();

        var loan =
            await CreateLoanAsync(
                setup.Context.Client,
                setup.InvestorId,
                setup.ClientId,
                900m);

        var response =
            await setup.Context.Client.PostAsJsonAsync(
                $"/api/loans/{loan.Id}/guarantee",
                new CreateLoanGuaranteeRequest
                {
                    Type =
                        LoanGuaranteeType.Guarantor,

                    Reference =
                        "GAR-001",

                    Description =
                        "Registered after loan creation"
                });

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var guarantee =
            await response.Content
                .ReadFromJsonAsync<
                    LoanGuaranteeResponse>();

        Assert.NotNull(
            guarantee);

        Assert.NotEqual(
            Guid.Empty,
            guarantee.Id);

        Assert.Equal(
            LoanGuaranteeType.Guarantor,
            guarantee.Type);

        Assert.Equal(
            "GAR-001",
            guarantee.Reference);

        var loanResponse =
            await setup.Context.Client.GetAsync(
                $"/api/loans/{loan.Id}");

        loanResponse.EnsureSuccessStatusCode();

        var storedLoan =
            await loanResponse.Content
                .ReadFromJsonAsync<LoanResponse>();

        Assert.NotNull(
            storedLoan);

        Assert.NotNull(
            storedLoan.Guarantee);

        Assert.Equal(
            guarantee.Id,
            storedLoan.Guarantee.Id);
    }

    [Fact]
    public async Task PostGuarantee_WhenAlreadyExists_ReturnsBadRequest()
    {
        var setup =
            await CreateBaseSetupAsync();

        var loan =
            await CreateLoanAsync(
                setup.Context.Client,
                setup.InvestorId,
                setup.ClientId,
                900m,
                new CreateLoanGuaranteeRequest
                {
                    Type =
                        LoanGuaranteeType.Other,

                    Reference =
                        "FIRST-GUARANTEE"
                });

        var response =
            await setup.Context.Client.PostAsJsonAsync(
                $"/api/loans/{loan.Id}/guarantee",
                new CreateLoanGuaranteeRequest
                {
                    Type =
                        LoanGuaranteeType.Collateral,

                    Reference =
                        "SECOND-GUARANTEE"
                });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task PutGuarantee_UpdatesGuarantee()
    {
        var setup =
            await CreateBaseSetupAsync();

        var loan =
            await CreateLoanAsync(
                setup.Context.Client,
                setup.InvestorId,
                setup.ClientId,
                900m,
                new CreateLoanGuaranteeRequest
                {
                    Type =
                        LoanGuaranteeType.Other,

                    Reference =
                        "OLD-REFERENCE",

                    Description =
                        "Old description"
                });

        var response =
            await setup.Context.Client.PutAsJsonAsync(
                $"/api/loans/{loan.Id}/guarantee",
                new UpdateLoanGuaranteeRequest
                {
                    Type =
                        LoanGuaranteeType.Collateral,

                    Reference =
                        "NEW-REFERENCE",

                    Description =
                        "Updated description"
                });

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var guarantee =
            await response.Content
                .ReadFromJsonAsync<
                    LoanGuaranteeResponse>();

        Assert.NotNull(
            guarantee);

        Assert.Equal(
            LoanGuaranteeType.Collateral,
            guarantee.Type);

        Assert.Equal(
            "NEW-REFERENCE",
            guarantee.Reference);

        Assert.Equal(
            "Updated description",
            guarantee.Description);
    }

    // ============================================================
    // MULTI-TENANT AND AUTHORIZATION
    // ============================================================

    [Fact]
    public async Task GuaranteeEndpoint_CannotAccessLoanFromAnotherTenant()
    {
        var tenant1 =
            await CreateContextAsync();

        var tenant2Setup =
            await CreateBaseSetupAsync();

        var loan =
            await CreateLoanAsync(
                tenant2Setup.Context.Client,
                tenant2Setup.InvestorId,
                tenant2Setup.ClientId,
                900m,
                new CreateLoanGuaranteeRequest
                {
                    Type =
                        LoanGuaranteeType.Other,

                    Reference =
                        "OTHER-TENANT"
                });

        var response =
            await tenant1.Client.GetAsync(
                $"/api/loans/{loan.Id}/guarantee");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task GuaranteeEndpoint_WithCollector_ReturnsForbidden()
    {
        var setup =
            await CreateBaseSetupAsync();

        var loan =
            await CreateLoanAsync(
                setup.Context.Client,
                setup.InvestorId,
                setup.ClientId,
                900m);

        var collector =
            await CreateCollectorAsync(
                setup.Context);

        var collectorClient =
            await LoginCollectorAsync(
                setup.Context.TenantId,
                collector);

        var response =
            await collectorClient.GetAsync(
                $"/api/loans/{loan.Id}/guarantee");

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    // ============================================================
    // LOAN UPDATE + GUARANTEE POLICY
    // ============================================================

    [Fact]
    public async Task UpdateLoan_CrossingThresholdWithoutGuarantee_ReturnsBadRequest()
    {
        var setup =
            await CreateBaseSetupAsync();

        await ConfigureGuaranteeThresholdAsync(
            setup.Context,
            1000m);

        var loan =
            await CreateLoanAsync(
                setup.Context.Client,
                setup.InvestorId,
                setup.ClientId,
                principalAmount: 900m);

        Assert.False(
            loan.GuaranteeRequired);

        var response =
            await UpdateLoanAsync(
                setup.Context.Client,
                loan,
                principalAmount: 1200m);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task UpdateLoan_AfterRegisteringGuarantee_CrossingThresholdSucceeds()
    {
        var setup =
            await CreateBaseSetupAsync();

        await ConfigureGuaranteeThresholdAsync(
            setup.Context,
            1000m);

        var loan =
            await CreateLoanAsync(
                setup.Context.Client,
                setup.InvestorId,
                setup.ClientId,
                900m);

        var guaranteeResponse =
            await setup.Context.Client.PostAsJsonAsync(
                $"/api/loans/{loan.Id}/guarantee",
                new CreateLoanGuaranteeRequest
                {
                    Type =
                        LoanGuaranteeType.Collateral,

                    Reference =
                        "CROSS-THRESHOLD"
                });

        Assert.Equal(
            HttpStatusCode.Created,
            guaranteeResponse.StatusCode);

        var updateResponse =
            await UpdateLoanAsync(
                setup.Context.Client,
                loan,
                1200m);

        Assert.Equal(
            HttpStatusCode.OK,
            updateResponse.StatusCode);

        var updatedLoan =
            await updateResponse.Content
                .ReadFromJsonAsync<LoanResponse>();

        Assert.NotNull(
            updatedLoan);

        Assert.True(
            updatedLoan.GuaranteeRequired);

        Assert.Equal(
            1000m,
            updatedLoan.GuaranteeThresholdAtCreation!.Value);

        Assert.NotNull(
            updatedLoan.Guarantee);
    }

    [Fact]
    public async Task UpdateLoan_BelowThreshold_KeepsExistingGuaranteeAndSetsRequiredFalse()
    {
        var setup =
            await CreateBaseSetupAsync();

        await ConfigureGuaranteeThresholdAsync(
            setup.Context,
            1000m);

        var loan =
            await CreateLoanAsync(
                setup.Context.Client,
                setup.InvestorId,
                setup.ClientId,
                1200m,
                new CreateLoanGuaranteeRequest
                {
                    Type =
                        LoanGuaranteeType.Collateral,

                    Reference =
                        "KEEP-GUARANTEE"
                });

        Assert.True(
            loan.GuaranteeRequired);

        var response =
            await UpdateLoanAsync(
                setup.Context.Client,
                loan,
                principalAmount: 900m);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var updated =
            await response.Content
                .ReadFromJsonAsync<LoanResponse>();

        Assert.NotNull(
            updated);

        Assert.False(
            updated.GuaranteeRequired);

        Assert.Equal(
            1000m,
            updated.GuaranteeThresholdAtCreation!.Value);

        Assert.NotNull(
            updated.Guarantee);

        Assert.Equal(
            "KEEP-GUARANTEE",
            updated.Guarantee.Reference);
    }

    // ============================================================
    // SETUP
    // ============================================================

    private async Task<BaseSetup>
        CreateBaseSetupAsync()
    {
        var context =
            await CreateContextAsync();

        var investorId =
            await CreateInvestorAsync(
                context.Client);

        var clientId =
            await CreateClientAsync(
                context.Client);

        return new BaseSetup(
            context,
            investorId,
            clientId);
    }

    private sealed record BaseSetup(
        TestTenantContext Context,
        Guid InvestorId,
        Guid ClientId);

    private sealed record CollectorCredentials(
        Guid Id,
        string Username,
        string Password);

    // ============================================================
    // TENANT HELPERS
    // ============================================================

    private static async Task
        ConfigureGuaranteeThresholdAsync(
            TestTenantContext context,
            decimal? threshold)
    {
        var response =
            await UpdateGuaranteeThresholdAsync(
                context,
                threshold);

        response.EnsureSuccessStatusCode();
    }

    private static async Task<HttpResponseMessage>
        UpdateGuaranteeThresholdAsync(
            TestTenantContext context,
            decimal? threshold)
    {
        return await context.Client.PutAsJsonAsync(
            "/api/tenants/me",
            new UpdateTenantRequest
            {
                Name =
                    $"Guarantee Tenant {Guid.NewGuid():N}",

                LegalName =
                    "Guarantee Integration Test SRL",

                Phone =
                    "8095551234",

                Email =
                    $"guarantee-{Guid.NewGuid():N}@sanes.test",

                CurrencyCode =
                    "DOP",

                CurrencySymbol =
                    "RD$",

                DefaultLateFeeEnabled =
                    false,

                DefaultLateFeeCalculationType =
                    LateFeeCalculationType
                        .FixedAmountPerInstallment,

                DefaultLateFeeAmount =
                    0m,

                DefaultLateFeeGraceDays =
                    0,

                GuaranteeRequiredFromAmount =
                    threshold
            });
    }

    // ============================================================
    // AUTHENTICATION
    // ============================================================

    private async Task<TestTenantContext>
        CreateContextAsync()
    {
        return await TestAuthenticationHelper
            .CreateAdministratorContextAsync(
                _factory);
    }

    private async Task<CollectorCredentials>
        CreateCollectorAsync(
            TestTenantContext context)
    {
        var suffix =
            Guid.NewGuid()
                .ToString("N");

        var username =
            $"guarantee-collector-{suffix}";

        var password =
            TestAuthenticationHelper
                .DefaultPassword;

        var response =
            await context.Client.PostAsJsonAsync(
                "/api/app-users",
                new
                {
                    name =
                        $"Guarantee Collector {suffix}",

                    username,

                    password,

                    email =
                        $"guarantee-{suffix}@sanes.test",

                    phone =
                        "8095551234",

                    role =
                        (int)AppUserRole.Collector
                });

        response.EnsureSuccessStatusCode();

        var json =
            await response.Content
                .ReadFromJsonAsync<JsonElement>();

        return new CollectorCredentials(
            json.GetProperty("id")
                .GetGuid(),
            username,
            password);
    }

    private async Task<HttpClient>
        LoginCollectorAsync(
            Guid tenantId,
            CollectorCredentials collector)
    {
        var client =
            _factory.CreateClient();

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/login",
                new LoginRequest
                {
                    TenantId =
                        tenantId,

                    Username =
                        collector.Username,

                    Password =
                        collector.Password
                });

        response.EnsureSuccessStatusCode();

        var auth =
            await response.Content
                .ReadFromJsonAsync<AuthResponse>();

        Assert.NotNull(
            auth);

        return TestAuthenticationHelper
            .CreateAuthenticatedClient(
                _factory,
                auth.AccessToken);
    }

    // ============================================================
    // BUSINESS HELPERS
    // ============================================================

    private static async Task<Guid>
        CreateInvestorAsync(
            HttpClient client)
    {
        var response =
            await client.PostAsJsonAsync(
                "/api/investors",
                new CreateInvestorRequest
                {
                    Name =
                        $"Guarantee Investor {Guid.NewGuid():N}",

                    Phone =
                        "8095551000",

                    Identification =
                        $"GI-{Guid.NewGuid():N}"
                });

        response.EnsureSuccessStatusCode();

        var investor =
            await response.Content
                .ReadFromJsonAsync<
                    InvestorResponse>();

        Assert.NotNull(
            investor);

        return investor.Id;
    }

    private static async Task<Guid>
        CreateClientAsync(
            HttpClient client)
    {
        var response =
            await client.PostAsJsonAsync(
                "/api/clients",
                new CreateClientRequest
                {
                    FirstName =
                        "Guarantee",

                    LastName =
                        $"Client {Guid.NewGuid():N}",

                    Phone =
                        $"809{Random.Shared.Next(
                            1000000,
                            9999999)}",

                    Address =
                        "Guarantee Integration Test"
                });

        response.EnsureSuccessStatusCode();

        var created =
            await response.Content
                .ReadFromJsonAsync<ClientResponse>();

        Assert.NotNull(
            created);

        return created.Id;
    }

    private static async Task<HttpResponseMessage>
        CreateLoanRawAsync(
            HttpClient client,
            Guid investorId,
            Guid clientId,
            decimal principalAmount,
            CreateLoanGuaranteeRequest? guarantee = null)
    {
        return await client.PostAsJsonAsync(
            "/api/loans",
            new CreateLoanRequest
            {
                InvestorId =
                    investorId,

                ClientId =
                    clientId,

                PrincipalAmount =
                    principalAmount,

                InstallmentAmount =
                    100m,

                TotalInstallments =
                    13,

                PaymentFrequency =
                    PaymentFrequency.Weekly,

                StartDate =
                    DateTime.UtcNow.Date,

                Notes =
                    "Loan guarantee integration test",

                Guarantee =
                    guarantee
            });
    }

    private static async Task<LoanResponse>
        CreateLoanAsync(
            HttpClient client,
            Guid investorId,
            Guid clientId,
            decimal principalAmount,
            CreateLoanGuaranteeRequest? guarantee = null)
    {
        var response =
            await CreateLoanRawAsync(
                client,
                investorId,
                clientId,
                principalAmount,
                guarantee);

        response.EnsureSuccessStatusCode();

        var loan =
            await response.Content
                .ReadFromJsonAsync<LoanResponse>();

        Assert.NotNull(
            loan);

        return loan;
    }

    private static async Task<HttpResponseMessage>
        UpdateLoanAsync(
            HttpClient client,
            LoanResponse loan,
            decimal principalAmount)
    {
        return await client.PutAsJsonAsync(
            $"/api/loans/{loan.Id}",
            new UpdateLoanRequest
            {
                PrincipalAmount =
                    principalAmount,

                InstallmentAmount =
                    loan.InstallmentAmount,

                TotalInstallments =
                    loan.TotalInstallments,

                PaymentFrequency =
                    loan.PaymentFrequency,

                StartDate =
                    loan.StartDate,

                Notes =
                    loan.Notes
            });
    }
}