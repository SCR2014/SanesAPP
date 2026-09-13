using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Sanes.Application.Clients.DTOs;
using Sanes.Application.CollectionRouteSchedules.DTOs;
using Sanes.Application.CollectionRoutes.DTOs;
using Sanes.Application.FieldCollections.DTOs;
using Sanes.Application.Investors.DTOs;
using Sanes.Application.Loans.DTOs;
using Sanes.Domain.Enums;

namespace Sanes.Api.IntegrationTests.FieldCollections;

public class FieldCollectionsTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public FieldCollectionsTests(
        CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetDaily_ForScheduledAssignedRoute_ReturnsOperationalPortfolio()
    {
        var tenantId = await CreateTenantAsync();
        var routeId = await CreateRouteAsync(tenantId);
        var collectorId = await CreateCollectorAsync(tenantId);

        await AssignRouteAsync(
            tenantId,
            collectorId,
            routeId);

        await CreateScheduleAsync(
            tenantId,
            routeId,
            CollectionWeekDay.Monday);

        var investorId =
            await CreateInvestorAsync(tenantId);

        var clientId =
            await CreateClientAsync(
                tenantId,
                routeId,
                "Field",
                "Client");

        var loan =
            await CreateLoanAsync(
                tenantId,
                investorId,
                clientId,
                new DateTime(
                    2026,
                    9,
                    7,
                    0,
                    0,
                    0,
                    DateTimeKind.Utc));

        var response =
            await GetDailyAsync(
                tenantId,
                collectorId,
                new DateOnly(2026, 9, 14));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<FieldCollectionDailyResponse>();

        Assert.NotNull(result);

        Assert.Equal(
            new DateOnly(2026, 9, 14),
            result.Date);

        Assert.Equal(
            collectorId,
            result.AppUserId);

        Assert.Equal(1, result.RoutesCount);
        Assert.Equal(1, result.ClientsCount);
        Assert.Equal(1, result.LoansCount);
        Assert.Equal(1300m, result.TotalBalance);
        Assert.Equal(100m, result.TotalAmountDue);

        var route = Assert.Single(result.Routes);

        Assert.Equal(
            routeId,
            route.CollectionRouteId);

        Assert.Equal(1, route.ClientsCount);
        Assert.Equal(1, route.LoansCount);
        Assert.Equal(1300m, route.TotalBalance);
        Assert.Equal(100m, route.TotalAmountDue);

        var client = Assert.Single(route.Clients);

        Assert.Equal(clientId, client.ClientId);
        Assert.Equal("Field", client.FirstName);
        Assert.Equal("Client", client.LastName);
        Assert.Equal(19.45m, client.Latitude);
        Assert.Equal(-70.69m, client.Longitude);

        var returnedLoan =
            Assert.Single(client.Loans);

        Assert.Equal(
            loan.Id,
            returnedLoan.LoanId);

        Assert.Equal(
            1300m,
            returnedLoan.Balance);

        Assert.Equal(
            100m,
            returnedLoan.InstallmentAmount);
    }

    [Fact]
    public async Task GetDaily_ForUnscheduledDay_ReturnsEmpty()
    {
        var tenantId = await CreateTenantAsync();
        var routeId = await CreateRouteAsync(tenantId);
        var collectorId = await CreateCollectorAsync(tenantId);

        await AssignRouteAsync(
            tenantId,
            collectorId,
            routeId);

        await CreateScheduleAsync(
            tenantId,
            routeId,
            CollectionWeekDay.Monday);

        var response =
            await GetDailyAsync(
                tenantId,
                collectorId,
                new DateOnly(2026, 9, 15));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<FieldCollectionDailyResponse>();

        Assert.NotNull(result);

        Assert.Equal(0, result.RoutesCount);
        Assert.Equal(0, result.ClientsCount);
        Assert.Equal(0, result.LoansCount);
        Assert.Equal(0m, result.TotalBalance);
        Assert.Equal(0m, result.TotalAmountDue);
        Assert.Empty(result.Routes);
    }

    [Fact]
    public async Task GetDaily_ReturnsOnlyRoutesAssignedToCollector()
    {
        var tenantId = await CreateTenantAsync();

        var route1 = await CreateRouteAsync(tenantId);
        var route2 = await CreateRouteAsync(tenantId);

        var collector1 =
            await CreateCollectorAsync(tenantId);

        var collector2 =
            await CreateCollectorAsync(tenantId);

        await AssignRouteAsync(
            tenantId,
            collector1,
            route1);

        await AssignRouteAsync(
            tenantId,
            collector2,
            route2);

        await CreateScheduleAsync(
            tenantId,
            route1,
            CollectionWeekDay.Monday);

        await CreateScheduleAsync(
            tenantId,
            route2,
            CollectionWeekDay.Monday);

        var response =
            await GetDailyAsync(
                tenantId,
                collector1,
                new DateOnly(2026, 9, 14));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<FieldCollectionDailyResponse>();

        Assert.NotNull(result);

        var route = Assert.Single(result.Routes);

        Assert.Equal(
            route1,
            route.CollectionRouteId);

        Assert.DoesNotContain(
            result.Routes,
            x => x.CollectionRouteId == route2);
    }

    [Fact]
    public async Task GetDaily_WithAdministrator_ReturnsBadRequest()
    {
        var tenantId = await CreateTenantAsync();

        var administratorId =
            await CreateAppUserAsync(
                tenantId,
                AppUserRole.Administrator);

        var response =
            await GetDailyAsync(
                tenantId,
                administratorId,
                new DateOnly(2026, 9, 14));

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task GetDaily_WithCollectorFromAnotherTenant_ReturnsBadRequest()
    {
        var tenant1 = await CreateTenantAsync();
        var tenant2 = await CreateTenantAsync();

        var collectorId =
            await CreateCollectorAsync(tenant1);

        var response =
            await GetDailyAsync(
                tenant2,
                collectorId,
                new DateOnly(2026, 9, 14));

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task GetDaily_ExcludesLoanNotDueByRequestedDate()
    {
        var tenantId = await CreateTenantAsync();
        var routeId = await CreateRouteAsync(tenantId);
        var collectorId = await CreateCollectorAsync(tenantId);

        await AssignRouteAsync(
            tenantId,
            collectorId,
            routeId);

        await CreateScheduleAsync(
            tenantId,
            routeId,
            CollectionWeekDay.Monday);

        var investorId =
            await CreateInvestorAsync(tenantId);

        var clientId =
            await CreateClientAsync(
                tenantId,
                routeId,
                "Future",
                "Loan");

        await CreateLoanAsync(
            tenantId,
            investorId,
            clientId,
            new DateTime(
                2026,
                9,
                14,
                0,
                0,
                0,
                DateTimeKind.Utc));

        /*
         * El préstamo semanal iniciado el 14-Sep
         * tendrá su próxima cuota después de la fecha
         * consultada.
         */
        var response =
            await GetDailyAsync(
                tenantId,
                collectorId,
                new DateOnly(2026, 9, 14));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<FieldCollectionDailyResponse>();

        Assert.NotNull(result);

        var route = Assert.Single(result.Routes);

        Assert.Empty(route.Clients);
        Assert.Equal(0, route.ClientsCount);
        Assert.Equal(0, route.LoansCount);
        Assert.Equal(0m, route.TotalAmountDue);
    }

    [Fact]
    public async Task GetDaily_OrdersClientsByCollectionRouteOrder()
    {
        var tenantId = await CreateTenantAsync();
        var routeId = await CreateRouteAsync(tenantId);
        var collectorId = await CreateCollectorAsync(tenantId);

        await AssignRouteAsync(
            tenantId,
            collectorId,
            routeId);

        await CreateScheduleAsync(
            tenantId,
            routeId,
            CollectionWeekDay.Monday);

        var investorId =
            await CreateInvestorAsync(tenantId);

        var client1 =
            await CreateClientAsync(
                tenantId,
                routeId,
                "First",
                "Client");

        var client2 =
            await CreateClientAsync(
                tenantId,
                routeId,
                "Second",
                "Client");

        var client3 =
            await CreateClientAsync(
                tenantId,
                routeId,
                "Third",
                "Client");

        await SetManualOrderAsync(
            tenantId,
            routeId,
            new[]
            {
                client3,
                client1,
                client2
            });

        var loanStart =
            new DateTime(
                2026,
                9,
                7,
                0,
                0,
                0,
                DateTimeKind.Utc);

        await CreateLoanAsync(
            tenantId,
            investorId,
            client1,
            loanStart);

        await CreateLoanAsync(
            tenantId,
            investorId,
            client2,
            loanStart);

        await CreateLoanAsync(
            tenantId,
            investorId,
            client3,
            loanStart);

        var response =
            await GetDailyAsync(
                tenantId,
                collectorId,
                new DateOnly(2026, 9, 14));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<FieldCollectionDailyResponse>();

        Assert.NotNull(result);

        var route = Assert.Single(result.Routes);

        Assert.Equal(3, route.Clients.Count);

        Assert.Equal(
            client3,
            route.Clients[0].ClientId);

        Assert.Equal(
            client1,
            route.Clients[1].ClientId);

        Assert.Equal(
            client2,
            route.Clients[2].ClientId);

        Assert.Equal(
            1,
            route.Clients[0].CollectionRouteOrder);

        Assert.Equal(
            2,
            route.Clients[1].CollectionRouteOrder);

        Assert.Equal(
            3,
            route.Clients[2].CollectionRouteOrder);
    }

    [Fact]
    public async Task CreatePayment_WithValidFieldContext_ReturnsReceipt()
    {
        var setup = await CreatePaymentScenarioAsync();

        var request =
            new CreateFieldCollectionPaymentRequest
            {
                TenantId = setup.TenantId,
                AppUserId = setup.CollectorId,
                CollectionRouteId = setup.RouteId,
                LoanId = setup.Loan.Id,
                Amount = 100m,
                PaymentType = PaymentType.Regular,
                Notes = "Cobrado en domicilio"
            };

        var response =
            await _client.PostAsJsonAsync(
                "/api/field-collections/payments",
                request);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var receipt =
            await response.Content
                .ReadFromJsonAsync<FieldCollectionPaymentResponse>();

        Assert.NotNull(receipt);

        Assert.NotEqual(Guid.Empty, receipt.PaymentId);
        Assert.Equal(setup.CollectorId, receipt.AppUserId);
        Assert.Equal(setup.RouteId, receipt.CollectionRouteId);
        Assert.Equal(setup.ClientId, receipt.ClientId);
        Assert.Equal(setup.Loan.Id, receipt.LoanId);

        Assert.Equal(100m, receipt.Amount);
        Assert.Equal(PaymentType.Regular, receipt.PaymentType);

        Assert.Equal(1300m, receipt.BalanceBefore);
        Assert.Equal(1200m, receipt.BalanceAfter);

        Assert.Equal(
            "Cobrado en domicilio",
            receipt.Notes);

        Assert.False(
            string.IsNullOrWhiteSpace(
                receipt.CollectorName));

        Assert.False(
            string.IsNullOrWhiteSpace(
                receipt.CollectionRouteName));

        Assert.False(
            string.IsNullOrWhiteSpace(
                receipt.ClientName));
    }

    [Fact]
    public async Task CreatePayment_PersistsCollectorAndRouteContext()
    {
        var setup = await CreatePaymentScenarioAsync();

        var request =
            new CreateFieldCollectionPaymentRequest
            {
                TenantId = setup.TenantId,
                AppUserId = setup.CollectorId,
                CollectionRouteId = setup.RouteId,
                LoanId = setup.Loan.Id,
                Amount = 100m,
                PaymentType = PaymentType.Regular
            };

        var response =
            await _client.PostAsJsonAsync(
                "/api/field-collections/payments",
                request);

        response.EnsureSuccessStatusCode();

        var receipt =
            await response.Content
                .ReadFromJsonAsync<FieldCollectionPaymentResponse>();

        Assert.NotNull(receipt);

        var paymentResponse =
            await _client.GetAsync(
                $"/api/payments/{receipt.PaymentId}" +
                $"?tenantId={setup.TenantId}");

        Assert.Equal(
            HttpStatusCode.OK,
            paymentResponse.StatusCode);

        var payment =
            await paymentResponse.Content
                .ReadFromJsonAsync<
                    Sanes.Application.Payments.DTOs.PaymentResponse>();

        Assert.NotNull(payment);

        Assert.Equal(
            setup.CollectorId,
            payment.CollectedByAppUserId);

        Assert.Equal(
            setup.RouteId,
            payment.CollectionRouteId);
    }

    [Fact]
    public async Task CreatePayment_UpdatesLoanFinancialState()
    {
        var setup = await CreatePaymentScenarioAsync();

        var originalNextPaymentDate =
            setup.Loan.NextPaymentDate;

        var request =
            new CreateFieldCollectionPaymentRequest
            {
                TenantId = setup.TenantId,
                AppUserId = setup.CollectorId,
                CollectionRouteId = setup.RouteId,
                LoanId = setup.Loan.Id,
                Amount = 100m,
                PaymentType = PaymentType.Regular
            };

        var response =
            await _client.PostAsJsonAsync(
                "/api/field-collections/payments",
                request);

        response.EnsureSuccessStatusCode();

        var receipt =
            await response.Content
                .ReadFromJsonAsync<FieldCollectionPaymentResponse>();

        Assert.NotNull(receipt);

        Assert.Equal(1300m, receipt.BalanceBefore);
        Assert.Equal(1200m, receipt.BalanceAfter);

        Assert.Equal(
            originalNextPaymentDate.AddDays(7),
            receipt.NextPaymentDate);
    }

    [Fact]
    public async Task CreatePayment_FullSettlement_MarksLoanAsPaid()
    {
        var setup = await CreatePaymentScenarioAsync();

        var request =
            new CreateFieldCollectionPaymentRequest
            {
                TenantId = setup.TenantId,
                AppUserId = setup.CollectorId,
                CollectionRouteId = setup.RouteId,
                LoanId = setup.Loan.Id,
                Amount = 1300m,
                PaymentType = PaymentType.FullSettlement
            };

        var response =
            await _client.PostAsJsonAsync(
                "/api/field-collections/payments",
                request);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var receipt =
            await response.Content
                .ReadFromJsonAsync<FieldCollectionPaymentResponse>();

        Assert.NotNull(receipt);

        Assert.Equal(1300m, receipt.BalanceBefore);
        Assert.Equal(0m, receipt.BalanceAfter);
        Assert.Null(receipt.NextPaymentDate);

        var loanResponse =
            await _client.GetAsync(
                $"/api/loans/{setup.Loan.Id}" +
                $"?tenantId={setup.TenantId}");

        Assert.Equal(
            HttpStatusCode.OK,
            loanResponse.StatusCode);

        var loan =
            await loanResponse.Content
                .ReadFromJsonAsync<LoanResponse>();

        Assert.NotNull(loan);

        Assert.Equal(
            LoanStatus.Paid,
            loan.Status);
    }

    [Fact]
    public async Task CreatePayment_Overpayment_ReturnsBadRequest()
    {
        var setup = await CreatePaymentScenarioAsync();

        var request =
            new CreateFieldCollectionPaymentRequest
            {
                TenantId = setup.TenantId,
                AppUserId = setup.CollectorId,
                CollectionRouteId = setup.RouteId,
                LoanId = setup.Loan.Id,
                Amount = 1400m,
                PaymentType = PaymentType.FullSettlement
            };

        var response =
            await _client.PostAsJsonAsync(
                "/api/field-collections/payments",
                request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task CreatePayment_WithCollectorNotAssignedToRoute_ReturnsBadRequest()
    {
        var tenantId = await CreateTenantAsync();
        var routeId = await CreateRouteAsync(tenantId);

        var collectorId =
            await CreateCollectorAsync(tenantId);

        var investorId =
            await CreateInvestorAsync(tenantId);

        var clientId =
            await CreateClientAsync(
                tenantId,
                routeId,
                "Unassigned",
                "Collector");

        var loan =
            await CreateLoanAsync(
                tenantId,
                investorId,
                clientId,
                DateTime.UtcNow.Date.AddDays(-7));

        var request =
            new CreateFieldCollectionPaymentRequest
            {
                TenantId = tenantId,
                AppUserId = collectorId,
                CollectionRouteId = routeId,
                LoanId = loan.Id,
                Amount = 100m,
                PaymentType = PaymentType.Regular
            };

        var response =
            await _client.PostAsJsonAsync(
                "/api/field-collections/payments",
                request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task CreatePayment_WhenLoanClientBelongsToDifferentRoute_ReturnsBadRequest()
    {
        var tenantId = await CreateTenantAsync();

        var assignedRoute =
            await CreateRouteAsync(tenantId);

        var clientRoute =
            await CreateRouteAsync(tenantId);

        var collectorId =
            await CreateCollectorAsync(tenantId);

        await AssignRouteAsync(
            tenantId,
            collectorId,
            assignedRoute);

        var investorId =
            await CreateInvestorAsync(tenantId);

        var clientId =
            await CreateClientAsync(
                tenantId,
                clientRoute,
                "Wrong",
                "Route");

        var loan =
            await CreateLoanAsync(
                tenantId,
                investorId,
                clientId,
                DateTime.UtcNow.Date.AddDays(-7));

        var request =
            new CreateFieldCollectionPaymentRequest
            {
                TenantId = tenantId,
                AppUserId = collectorId,
                CollectionRouteId = assignedRoute,
                LoanId = loan.Id,
                Amount = 100m,
                PaymentType = PaymentType.Regular
            };

        var response =
            await _client.PostAsJsonAsync(
                "/api/field-collections/payments",
                request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task CreatePayment_WithAdministrator_ReturnsBadRequest()
    {
        var tenantId = await CreateTenantAsync();
        var routeId = await CreateRouteAsync(tenantId);

        var administratorId =
            await CreateAppUserAsync(
                tenantId,
                AppUserRole.Administrator);

        var investorId =
            await CreateInvestorAsync(tenantId);

        var clientId =
            await CreateClientAsync(
                tenantId,
                routeId,
                "Admin",
                "Payment");

        var loan =
            await CreateLoanAsync(
                tenantId,
                investorId,
                clientId,
                DateTime.UtcNow.Date.AddDays(-7));

        var request =
            new CreateFieldCollectionPaymentRequest
            {
                TenantId = tenantId,
                AppUserId = administratorId,
                CollectionRouteId = routeId,
                LoanId = loan.Id,
                Amount = 100m,
                PaymentType = PaymentType.Regular
            };

        var response =
            await _client.PostAsJsonAsync(
                "/api/field-collections/payments",
                request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task CreatePayment_WithCollectorFromAnotherTenant_ReturnsBadRequest()
    {
        var tenant1 = await CreateTenantAsync();
        var tenant2 = await CreateTenantAsync();

        var collectorId =
            await CreateCollectorAsync(tenant1);

        var routeId =
            await CreateRouteAsync(tenant2);

        var investorId =
            await CreateInvestorAsync(tenant2);

        var clientId =
            await CreateClientAsync(
                tenant2,
                routeId,
                "Other",
                "Tenant");

        var loan =
            await CreateLoanAsync(
                tenant2,
                investorId,
                clientId,
                DateTime.UtcNow.Date.AddDays(-7));

        var request =
            new CreateFieldCollectionPaymentRequest
            {
                TenantId = tenant2,
                AppUserId = collectorId,
                CollectionRouteId = routeId,
                LoanId = loan.Id,
                Amount = 100m,
                PaymentType = PaymentType.Regular
            };

        var response =
            await _client.PostAsJsonAsync(
                "/api/field-collections/payments",
                request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    private async Task<PaymentScenario> CreatePaymentScenarioAsync()
    {
        var tenantId =
            await CreateTenantAsync();

        var routeId =
            await CreateRouteAsync(tenantId);

        var collectorId =
            await CreateCollectorAsync(tenantId);

        await AssignRouteAsync(
            tenantId,
            collectorId,
            routeId);

        var investorId =
            await CreateInvestorAsync(tenantId);

        var clientId =
            await CreateClientAsync(
                tenantId,
                routeId,
                "Payment",
                "Client");

        var loan =
            await CreateLoanAsync(
                tenantId,
                investorId,
                clientId,
                DateTime.UtcNow.Date.AddDays(-7));

        return new PaymentScenario(
            tenantId,
            routeId,
            collectorId,
            clientId,
            loan);
    }

    private sealed record PaymentScenario(
        Guid TenantId,
        Guid RouteId,
        Guid CollectorId,
        Guid ClientId,
        LoanResponse Loan);

    private async Task<HttpResponseMessage> GetDailyAsync(
        Guid tenantId,
        Guid appUserId,
        DateOnly date)
    {
        return await _client.GetAsync(
            $"/api/field-collections/daily" +
            $"?tenantId={tenantId}" +
            $"&appUserId={appUserId}" +
            $"&date={date:yyyy-MM-dd}");
    }

    private async Task<Guid> CreateTenantAsync()
    {
        var suffix =
            Guid.NewGuid().ToString("N");

        var request = new
        {
            name = $"Field Tenant {suffix}",
            legalName = $"Field Tenant {suffix}",
            phone = "8095551234",
            email = $"field-{suffix}@example.com",
            currencyCode = "DOP",
            currencySymbol = "RD$"
        };

        var response =
            await _client.PostAsJsonAsync(
                "/api/tenants",
                request);

        response.EnsureSuccessStatusCode();

        var json =
            await response.Content
                .ReadFromJsonAsync<JsonElement>();

        return json
            .GetProperty("id")
            .GetGuid();
    }

    private async Task<Guid> CreateRouteAsync(
        Guid tenantId)
    {
        var request =
            new CreateCollectionRouteRequest
            {
                TenantId = tenantId,
                Name =
                    $"Field Route {Guid.NewGuid():N}",
                Description =
                    "Field collection integration test"
            };

        var response =
            await _client.PostAsJsonAsync(
                "/api/collection-routes",
                request);

        response.EnsureSuccessStatusCode();

        var route =
            await response.Content
                .ReadFromJsonAsync<CollectionRouteResponse>();

        Assert.NotNull(route);

        return route.Id;
    }

    private async Task<Guid> CreateCollectorAsync(
        Guid tenantId)
    {
        return await CreateAppUserAsync(
            tenantId,
            AppUserRole.Collector);
    }

    private async Task<Guid> CreateAppUserAsync(
        Guid tenantId,
        AppUserRole role)
    {
        var suffix =
            Guid.NewGuid().ToString("N");

        var request = new
        {
            tenantId,
            name = $"Field User {suffix}",
            username = $"field-{suffix}",
            email =
                $"field-user-{suffix}@example.com",
            phone = "8095551234",
            role = (int)role
        };

        var response =
            await _client.PostAsJsonAsync(
                "/api/app-users",
                request);

        response.EnsureSuccessStatusCode();

        var json =
            await response.Content
                .ReadFromJsonAsync<JsonElement>();

        return json
            .GetProperty("id")
            .GetGuid();
    }

    private async Task AssignRouteAsync(
        Guid tenantId,
        Guid collectorId,
        Guid routeId)
    {
        var response =
            await _client.PostAsync(
                $"/api/app-users/{collectorId}" +
                $"/collection-routes/{routeId}" +
                $"?tenantId={tenantId}",
                null);

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);
    }

    private async Task CreateScheduleAsync(
        Guid tenantId,
        Guid routeId,
        CollectionWeekDay day)
    {
        var request =
            new CreateCollectionRouteScheduleRequest
            {
                TenantId = tenantId,
                CollectionRouteId = routeId,
                DayOfWeek = day
            };

        var response =
            await _client.PostAsJsonAsync(
                "/api/collection-route-schedules",
                request);

        response.EnsureSuccessStatusCode();
    }

    private async Task<Guid> CreateInvestorAsync(
        Guid tenantId)
    {
        var request =
            new CreateInvestorRequest
            {
                TenantId = tenantId,
                Name =
                    $"Field Investor {Guid.NewGuid():N}",
                Phone = "8095551000",
                Identification =
                    $"INV-{Guid.NewGuid():N}"
            };

        var response =
            await _client.PostAsJsonAsync(
                "/api/investors",
                request);

        response.EnsureSuccessStatusCode();

        var investor =
            await response.Content
                .ReadFromJsonAsync<InvestorResponse>();

        Assert.NotNull(investor);

        return investor.Id;
    }

    private async Task<Guid> CreateClientAsync(
        Guid tenantId,
        Guid routeId,
        string firstName,
        string lastName)
    {
        var request =
            new CreateClientRequest
            {
                TenantId = tenantId,
                FirstName = firstName,
                LastName = lastName,
                Phone =
                    $"809{Random.Shared.Next(
                        1000000,
                        9999999)}",
                Address =
                    "Field Collection Test Address",
                Latitude = 19.45m,
                Longitude = -70.69m,
                CollectionRouteId = routeId
            };

        var response =
            await _client.PostAsJsonAsync(
                "/api/clients",
                request);

        response.EnsureSuccessStatusCode();

        var client =
            await response.Content
                .ReadFromJsonAsync<ClientResponse>();

        Assert.NotNull(client);

        return client.Id;
    }

    private async Task<LoanResponse> CreateLoanAsync(
        Guid tenantId,
        Guid investorId,
        Guid clientId,
        DateTime startDate)
    {
        var request =
            new CreateLoanRequest
            {
                TenantId = tenantId,
                InvestorId = investorId,
                ClientId = clientId,
                PrincipalAmount = 1000m,
                InstallmentAmount = 100m,
                TotalInstallments = 13,
                PaymentFrequency =
                    PaymentFrequency.Weekly,
                StartDate = startDate,
                Notes =
                    "Field collection integration test"
            };

        var response =
            await _client.PostAsJsonAsync(
                "/api/loans",
                request);

        response.EnsureSuccessStatusCode();

        var loan =
            await response.Content
                .ReadFromJsonAsync<LoanResponse>();

        Assert.NotNull(loan);

        return loan;
    }

    private async Task SetManualOrderAsync(
        Guid tenantId,
        Guid routeId,
        IReadOnlyCollection<Guid> clientIds)
    {
        var request = new
        {
            clientIds
        };

        var response =
            await _client.PutAsJsonAsync(
                $"/api/collection-routes/{routeId}/clients/order" +
                $"?tenantId={tenantId}",
                request);

        response.EnsureSuccessStatusCode();
    }
}