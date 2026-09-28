using System.Net;
using System.Net.Http.Json;
using Sanes.Api.IntegrationTests.Helpers;
using Sanes.Application.Clients.DTOs;
using Sanes.Application.Investors.DTOs;
using Sanes.Application.Loans.DTOs;
using Sanes.Domain.Enums;

namespace Sanes.Api.IntegrationTests.Loans;

public class LoansTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public LoansTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Create_ReturnsClientAndInvestorNames()
    {
        var context = await CreateContextAsync();

        var investorName =
            $"Investor Loan {Guid.NewGuid():N}";

        var investor =
            await CreateInvestorAsync(
                context.Client,
                investorName);

        var clientFirstName =
            "Cliente";

        var clientLastName =
            $"Prestamo {Guid.NewGuid():N}";

        var client =
            await CreateClientAsync(
                context.Client,
                clientFirstName,
                clientLastName);

        var loan =
            await CreateLoanAsync(
                context.Client,
                investor.Id,
                client.Id);

        Assert.Equal(
            investorName,
            loan.InvestorName);

        Assert.Equal(
            $"{clientFirstName} {clientLastName}",
            loan.ClientName);
    }

    [Fact]
    public async Task GetAll_ReturnsClientAndInvestorNames()
    {
        var context = await CreateContextAsync();

        var investorName =
            $"Investor List {Guid.NewGuid():N}";

        var investor =
            await CreateInvestorAsync(
                context.Client,
                investorName);

        var clientFirstName =
            "Cliente";

        var clientLastName =
            $"Listado {Guid.NewGuid():N}";

        var client =
            await CreateClientAsync(
                context.Client,
                clientFirstName,
                clientLastName);

        var createdLoan =
            await CreateLoanAsync(
                context.Client,
                investor.Id,
                client.Id);

        var response =
            await context.Client.GetAsync(
                "/api/loans");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var loans =
            await response.Content
                .ReadFromJsonAsync<
                    List<LoanResponse>>();

        Assert.NotNull(loans);

        var loan =
            Assert.Single(
                loans,
                x => x.Id == createdLoan.Id);

        Assert.Equal(
            investorName,
            loan.InvestorName);

        Assert.Equal(
            $"{clientFirstName} {clientLastName}",
            loan.ClientName);
    }

    [Fact]
    public async Task GetById_ReturnsClientAndInvestorNames()
    {
        var context = await CreateContextAsync();

        var investorName =
            $"Investor Detail {Guid.NewGuid():N}";

        var investor =
            await CreateInvestorAsync(
                context.Client,
                investorName);

        var clientFirstName =
            "Cliente";

        var clientLastName =
            $"Detalle {Guid.NewGuid():N}";

        var client =
            await CreateClientAsync(
                context.Client,
                clientFirstName,
                clientLastName);

        var createdLoan =
            await CreateLoanAsync(
                context.Client,
                investor.Id,
                client.Id);

        var response =
            await context.Client.GetAsync(
                $"/api/loans/{createdLoan.Id}");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var loan =
            await response.Content
                .ReadFromJsonAsync<
                    LoanResponse>();

        Assert.NotNull(loan);

        Assert.Equal(
            investorName,
            loan.InvestorName);

        Assert.Equal(
            $"{clientFirstName} {clientLastName}",
            loan.ClientName);
    }

    [Fact]
    public async Task GetById_FromDifferentTenant_ReturnsNotFound()
    {
        var tenant1 =
            await CreateContextAsync();

        var tenant2 =
            await CreateContextAsync();

        var investor =
            await CreateInvestorAsync(
                tenant2.Client,
                $"Investor Tenant2 {Guid.NewGuid():N}");

        var client =
            await CreateClientAsync(
                tenant2.Client,
                "Cliente",
                $"Tenant2 {Guid.NewGuid():N}");

        var loan =
            await CreateLoanAsync(
                tenant2.Client,
                investor.Id,
                client.Id);

        var response =
            await tenant1.Client.GetAsync(
                $"/api/loans/{loan.Id}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    private async Task<TestTenantContext>
        CreateContextAsync()
    {
        return await TestAuthenticationHelper
            .CreateAdministratorContextAsync(
                _factory);
    }

    private static async Task<InvestorResponse>
        CreateInvestorAsync(
            HttpClient client,
            string name)
    {
        var request =
            new CreateInvestorRequest
            {
                Name = name,
                Phone = "8095551000",
                Identification =
                    $"INV-{Guid.NewGuid():N}"
            };

        var response =
            await client.PostAsJsonAsync(
                "/api/investors",
                request);

        response.EnsureSuccessStatusCode();

        var investor =
            await response.Content
                .ReadFromJsonAsync<
                    InvestorResponse>();

        Assert.NotNull(investor);

        return investor;
    }

    private static async Task<ClientResponse>
        CreateClientAsync(
            HttpClient client,
            string firstName,
            string lastName)
    {
        var request =
            new CreateClientRequest
            {
                FirstName = firstName,
                LastName = lastName,
                Phone =
                    $"809{Random.Shared.Next(
                        1000000,
                        9999999)}"
            };

        var response =
            await client.PostAsJsonAsync(
                "/api/clients",
                request);

        response.EnsureSuccessStatusCode();

        var createdClient =
            await response.Content
                .ReadFromJsonAsync<
                    ClientResponse>();

        Assert.NotNull(createdClient);

        return createdClient;
    }

    private static async Task<LoanResponse>
        CreateLoanAsync(
            HttpClient client,
            Guid investorId,
            Guid clientId)
    {
        var request =
            new CreateLoanRequest
            {
                InvestorId =
                    investorId,

                ClientId =
                    clientId,

                PrincipalAmount =
                    100m,

                InstallmentAmount =
                    10m,

                TotalInstallments =
                    13,

                PaymentFrequency =
                    PaymentFrequency.Weekly,

                StartDate =
                    DateTime.UtcNow.Date,

                Notes =
                    "Loan creado por integration test"
            };

        var response =
            await client.PostAsJsonAsync(
                "/api/loans",
                request);

        response.EnsureSuccessStatusCode();

        var loan =
            await response.Content
                .ReadFromJsonAsync<
                    LoanResponse>();

        Assert.NotNull(loan);

        return loan;
    }
}