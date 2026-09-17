using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Sanes.Api.IntegrationTests.Helpers;
using Sanes.Application.Authentication.DTOs;
using Sanes.Application.Clients.DTOs;
using Sanes.Application.CollectionRouteSchedules.DTOs;
using Sanes.Application.CollectionRoutes.DTOs;
using Sanes.Application.FieldCollections.DTOs;
using Sanes.Application.Investors.DTOs;
using Sanes.Application.Loans.DTOs;
using Sanes.Application.Payments.DTOs;
using Sanes.Domain.Enums;

namespace Sanes.Api.IntegrationTests.FieldCollections;

public class FieldCollectionsTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public FieldCollectionsTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ============================================================
    // DAILY COLLECTION
    // ============================================================

    [Fact]
    public async Task GetDaily_ForScheduledAssignedRoute_ReturnsOperationalPortfolio()
    {
        var context = await CreateContextAsync();

        var routeId =
            await CreateRouteAsync(
                context.Client);

        var collector =
            await CreateCollectorAsync(
                context);

        await AssignRouteAsync(
            context.Client,
            collector.Id,
            routeId);

        await CreateScheduleAsync(
            context.Client,
            routeId,
            CollectionWeekDay.Monday);

        var investorId =
            await CreateInvestorAsync(
                context.Client);

        var clientId =
            await CreateClientAsync(
                context.Client,
                routeId,
                "Field",
                "Client");

        var loan =
            await CreateLoanAsync(
                context.Client,
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

        var collectorClient =
            await LoginCollectorAsync(
                context.TenantId,
                collector);

        var response =
            await GetDailyAsync(
                collectorClient,
                new DateOnly(2026, 9, 14));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    FieldCollectionDailyResponse>();

        Assert.NotNull(result);

        Assert.Equal(
            new DateOnly(2026, 9, 14),
            result.Date);

        Assert.Equal(
            collector.Id,
            result.AppUserId);

        Assert.Equal(1, result.RoutesCount);
        Assert.Equal(1, result.ClientsCount);
        Assert.Equal(1, result.LoansCount);
        Assert.Equal(1300m, result.TotalBalance);
        Assert.Equal(100m, result.TotalAmountDue);

        var route =
            Assert.Single(
                result.Routes);

        Assert.Equal(
            routeId,
            route.CollectionRouteId);

        Assert.Equal(1, route.ClientsCount);
        Assert.Equal(1, route.LoansCount);
        Assert.Equal(1300m, route.TotalBalance);
        Assert.Equal(100m, route.TotalAmountDue);

        var client =
            Assert.Single(
                route.Clients);

        Assert.Equal(
            clientId,
            client.ClientId);

        Assert.Equal(
            "Field",
            client.FirstName);

        Assert.Equal(
            "Client",
            client.LastName);

        Assert.Equal(
            19.45m,
            client.Latitude);

        Assert.Equal(
            -70.69m,
            client.Longitude);

        var returnedLoan =
            Assert.Single(
                client.Loans);

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
        var context = await CreateContextAsync();

        var routeId =
            await CreateRouteAsync(
                context.Client);

        var collector =
            await CreateCollectorAsync(
                context);

        await AssignRouteAsync(
            context.Client,
            collector.Id,
            routeId);

        await CreateScheduleAsync(
            context.Client,
            routeId,
            CollectionWeekDay.Monday);

        var collectorClient =
            await LoginCollectorAsync(
                context.TenantId,
                collector);

        var response =
            await GetDailyAsync(
                collectorClient,
                new DateOnly(2026, 9, 15));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    FieldCollectionDailyResponse>();

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
        var context = await CreateContextAsync();

        var route1 =
            await CreateRouteAsync(
                context.Client);

        var route2 =
            await CreateRouteAsync(
                context.Client);

        var collector1 =
            await CreateCollectorAsync(
                context);

        var collector2 =
            await CreateCollectorAsync(
                context);

        await AssignRouteAsync(
            context.Client,
            collector1.Id,
            route1);

        await AssignRouteAsync(
            context.Client,
            collector2.Id,
            route2);

        await CreateScheduleAsync(
            context.Client,
            route1,
            CollectionWeekDay.Monday);

        await CreateScheduleAsync(
            context.Client,
            route2,
            CollectionWeekDay.Monday);

        var collectorClient =
            await LoginCollectorAsync(
                context.TenantId,
                collector1);

        var response =
            await GetDailyAsync(
                collectorClient,
                new DateOnly(2026, 9, 14));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    FieldCollectionDailyResponse>();

        Assert.NotNull(result);

        var route =
            Assert.Single(
                result.Routes);

        Assert.Equal(
            route1,
            route.CollectionRouteId);

        Assert.DoesNotContain(
            result.Routes,
            x =>
                x.CollectionRouteId ==
                route2);
    }

    [Fact]
    public async Task GetDaily_WithAdministrator_ReturnsForbidden()
    {
        var context = await CreateContextAsync();

        var response =
            await GetDailyAsync(
                context.Client,
                new DateOnly(2026, 9, 14));

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task GetDaily_CollectorCannotAccessAnotherTenantData()
    {
        var tenant1 = await CreateContextAsync();
        var tenant2 = await CreateContextAsync();

        var collector =
            await CreateCollectorAsync(
                tenant1);

        var collectorClient =
            await LoginCollectorAsync(
                tenant1.TenantId,
                collector);

        var routeTenant2 =
            await CreateRouteAsync(
                tenant2.Client);

        await CreateScheduleAsync(
            tenant2.Client,
            routeTenant2,
            CollectionWeekDay.Monday);

        var response =
            await GetDailyAsync(
                collectorClient,
                new DateOnly(2026, 9, 14));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    FieldCollectionDailyResponse>();

        Assert.NotNull(result);

        Assert.DoesNotContain(
            result.Routes,
            x =>
                x.CollectionRouteId ==
                routeTenant2);
    }

    [Fact]
    public async Task GetDaily_ExcludesLoanNotDueByRequestedDate()
    {
        var context = await CreateContextAsync();

        var routeId =
            await CreateRouteAsync(
                context.Client);

        var collector =
            await CreateCollectorAsync(
                context);

        await AssignRouteAsync(
            context.Client,
            collector.Id,
            routeId);

        await CreateScheduleAsync(
            context.Client,
            routeId,
            CollectionWeekDay.Monday);

        var investorId =
            await CreateInvestorAsync(
                context.Client);

        var clientId =
            await CreateClientAsync(
                context.Client,
                routeId,
                "Future",
                "Loan");

        await CreateLoanAsync(
            context.Client,
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

        var collectorClient =
            await LoginCollectorAsync(
                context.TenantId,
                collector);

        var response =
            await GetDailyAsync(
                collectorClient,
                new DateOnly(2026, 9, 14));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    FieldCollectionDailyResponse>();

        Assert.NotNull(result);

        var route =
            Assert.Single(
                result.Routes);

        Assert.Empty(route.Clients);
        Assert.Equal(0, route.ClientsCount);
        Assert.Equal(0, route.LoansCount);
        Assert.Equal(0m, route.TotalAmountDue);
    }

    [Fact]
    public async Task GetDaily_OrdersClientsByCollectionRouteOrder()
    {
        var context = await CreateContextAsync();

        var routeId =
            await CreateRouteAsync(
                context.Client);

        var collector =
            await CreateCollectorAsync(
                context);

        await AssignRouteAsync(
            context.Client,
            collector.Id,
            routeId);

        await CreateScheduleAsync(
            context.Client,
            routeId,
            CollectionWeekDay.Monday);

        var investorId =
            await CreateInvestorAsync(
                context.Client);

        var client1 =
            await CreateClientAsync(
                context.Client,
                routeId,
                "First",
                "Client");

        var client2 =
            await CreateClientAsync(
                context.Client,
                routeId,
                "Second",
                "Client");

        var client3 =
            await CreateClientAsync(
                context.Client,
                routeId,
                "Third",
                "Client");

        await SetManualOrderAsync(
            context.Client,
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
            context.Client,
            investorId,
            client1,
            loanStart);

        await CreateLoanAsync(
            context.Client,
            investorId,
            client2,
            loanStart);

        await CreateLoanAsync(
            context.Client,
            investorId,
            client3,
            loanStart);

        var collectorClient =
            await LoginCollectorAsync(
                context.TenantId,
                collector);

        var response =
            await GetDailyAsync(
                collectorClient,
                new DateOnly(2026, 9, 14));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    FieldCollectionDailyResponse>();

        Assert.NotNull(result);

        var route =
            Assert.Single(
                result.Routes);

        Assert.Equal(
            3,
            route.Clients.Count);

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
            route.Clients[0]
                .CollectionRouteOrder);

        Assert.Equal(
            2,
            route.Clients[1]
                .CollectionRouteOrder);

        Assert.Equal(
            3,
            route.Clients[2]
                .CollectionRouteOrder);
    }

    // ============================================================
    // FIELD PAYMENTS
    // ============================================================

    [Fact]
    public async Task CreatePayment_WithValidFieldContext_ReturnsReceipt()
    {
        var setup =
            await CreatePaymentScenarioAsync();

        var request =
            CreatePaymentRequest(
                setup,
                100m,
                PaymentType.Regular,
                "Cobrado en domicilio");

        var response =
            await setup.CollectorClient
                .PostAsJsonAsync(
                    "/api/field-collections/payments",
                    request);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var receipt =
            await response.Content
                .ReadFromJsonAsync<
                    FieldCollectionPaymentResponse>();

        Assert.NotNull(receipt);

        Assert.NotEqual(
            Guid.Empty,
            receipt.PaymentId);

        Assert.Equal(
            setup.Collector.Id,
            receipt.AppUserId);

        Assert.Equal(
            setup.RouteId,
            receipt.CollectionRouteId);

        Assert.Equal(
            setup.ClientId,
            receipt.ClientId);

        Assert.Equal(
            setup.Loan.Id,
            receipt.LoanId);

        Assert.Equal(
            100m,
            receipt.Amount);

        Assert.Equal(
            0m,
            receipt.AppliedToLateFees);

        Assert.Equal(
            100m,
            receipt.AppliedToLoan);

        Assert.Equal(
            0m,
            receipt.LateFeeBalanceBefore);

        Assert.Equal(
            0m,
            receipt.LateFeeBalanceAfter);

        Assert.Equal(
            1300m,
            receipt.TotalOutstandingBefore);

        Assert.Equal(
            1200m,
            receipt.TotalOutstandingAfter);

        Assert.Equal(
            PaymentType.Regular,
            receipt.PaymentType);

        Assert.Equal(
            1300m,
            receipt.BalanceBefore);

        Assert.Equal(
            1200m,
            receipt.BalanceAfter);

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
        var setup =
            await CreatePaymentScenarioAsync();

        var request =
            CreatePaymentRequest(
                setup,
                100m,
                PaymentType.Regular);

        var response =
            await setup.CollectorClient
                .PostAsJsonAsync(
                    "/api/field-collections/payments",
                    request);

        response.EnsureSuccessStatusCode();

        var receipt =
            await response.Content
                .ReadFromJsonAsync<
                    FieldCollectionPaymentResponse>();

        Assert.NotNull(receipt);

        // Payments es un endpoint administrativo.
        var paymentResponse =
            await setup.AdministratorClient.GetAsync(
                $"/api/payments/{receipt.PaymentId}");

        Assert.Equal(
            HttpStatusCode.OK,
            paymentResponse.StatusCode);

        var payment =
            await paymentResponse.Content
                .ReadFromJsonAsync<
                    Sanes.Application.Payments.DTOs.PaymentResponse>();

        Assert.NotNull(payment);

        Assert.Equal(
            setup.Collector.Id,
            payment.CollectedByAppUserId);

        Assert.Equal(
            setup.RouteId,
            payment.CollectionRouteId);
    }

    [Fact]
    public async Task CreatePayment_UpdatesLoanFinancialState()
    {
        var setup =
            await CreatePaymentScenarioAsync();

        var originalNextPaymentDate =
            setup.Loan.NextPaymentDate;

        var request =
            CreatePaymentRequest(
                setup,
                100m,
                PaymentType.Regular);

        var response =
            await setup.CollectorClient
                .PostAsJsonAsync(
                    "/api/field-collections/payments",
                    request);

        response.EnsureSuccessStatusCode();

        var receipt =
            await response.Content
                .ReadFromJsonAsync<
                    FieldCollectionPaymentResponse>();

        Assert.NotNull(receipt);

        Assert.Equal(
            1300m,
            receipt.BalanceBefore);

        Assert.Equal(
            1200m,
            receipt.BalanceAfter);

        Assert.Equal(
            originalNextPaymentDate.AddDays(7),
            receipt.NextPaymentDate);
    }

    [Fact]
    public async Task CreatePayment_FullSettlement_MarksLoanAsPaid()
    {
        var setup =
            await CreatePaymentScenarioAsync();

        var request =
            CreatePaymentRequest(
                setup,
                1300m,
                PaymentType.FullSettlement);

        var response =
            await setup.CollectorClient
                .PostAsJsonAsync(
                    "/api/field-collections/payments",
                    request);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var receipt =
            await response.Content
                .ReadFromJsonAsync<
                    FieldCollectionPaymentResponse>();

        Assert.NotNull(receipt);

        Assert.Equal(
            1300m,
            receipt.BalanceBefore);

        Assert.Equal(
            0m,
            receipt.BalanceAfter);

        Assert.Null(
            receipt.NextPaymentDate);

        var loanResponse =
            await setup.AdministratorClient.GetAsync(
                $"/api/loans/{setup.Loan.Id}");

        Assert.Equal(
            HttpStatusCode.OK,
            loanResponse.StatusCode);

        var loan =
            await loanResponse.Content
                .ReadFromJsonAsync<
                    LoanResponse>();

        Assert.NotNull(loan);

        Assert.Equal(
            LoanStatus.Paid,
            loan.Status);
    }

    [Fact]
    public async Task CreatePayment_Overpayment_ReturnsBadRequest()
    {
        var setup =
            await CreatePaymentScenarioAsync();

        var request =
            CreatePaymentRequest(
                setup,
                1400m,
                PaymentType.FullSettlement);

        var response =
            await setup.CollectorClient
                .PostAsJsonAsync(
                    "/api/field-collections/payments",
                    request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task CreatePayment_WithCollectorNotAssignedToRoute_ReturnsBadRequest()
    {
        var context = await CreateContextAsync();

        var routeId =
            await CreateRouteAsync(
                context.Client);

        var collector =
            await CreateCollectorAsync(
                context);

        var investorId =
            await CreateInvestorAsync(
                context.Client);

        var clientId =
            await CreateClientAsync(
                context.Client,
                routeId,
                "Unassigned",
                "Collector");

        var loan =
            await CreateLoanAsync(
                context.Client,
                investorId,
                clientId,
                DateTime.UtcNow.Date
                    .AddDays(-7));

        var collectorClient =
            await LoginCollectorAsync(
                context.TenantId,
                collector);

        var request =
            new CreateFieldCollectionPaymentRequest
            {
                CollectionRouteId = routeId,
                LoanId = loan.Id,
                Amount = 100m,
                PaymentType =
                    PaymentType.Regular
            };

        var response =
            await collectorClient.PostAsJsonAsync(
                "/api/field-collections/payments",
                request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task CreatePayment_WhenLoanClientBelongsToDifferentRoute_ReturnsBadRequest()
    {
        var context = await CreateContextAsync();

        var assignedRoute =
            await CreateRouteAsync(
                context.Client);

        var clientRoute =
            await CreateRouteAsync(
                context.Client);

        var collector =
            await CreateCollectorAsync(
                context);

        await AssignRouteAsync(
            context.Client,
            collector.Id,
            assignedRoute);

        var investorId =
            await CreateInvestorAsync(
                context.Client);

        var clientId =
            await CreateClientAsync(
                context.Client,
                clientRoute,
                "Wrong",
                "Route");

        var loan =
            await CreateLoanAsync(
                context.Client,
                investorId,
                clientId,
                DateTime.UtcNow.Date
                    .AddDays(-7));

        var collectorClient =
            await LoginCollectorAsync(
                context.TenantId,
                collector);

        var request =
            new CreateFieldCollectionPaymentRequest
            {
                CollectionRouteId =
                    assignedRoute,
                LoanId = loan.Id,
                Amount = 100m,
                PaymentType =
                    PaymentType.Regular
            };

        var response =
            await collectorClient.PostAsJsonAsync(
                "/api/field-collections/payments",
                request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task CreatePayment_WithAdministrator_ReturnsForbidden()
    {
        var context = await CreateContextAsync();

        var routeId =
            await CreateRouteAsync(
                context.Client);

        var investorId =
            await CreateInvestorAsync(
                context.Client);

        var clientId =
            await CreateClientAsync(
                context.Client,
                routeId,
                "Admin",
                "Payment");

        var loan =
            await CreateLoanAsync(
                context.Client,
                investorId,
                clientId,
                DateTime.UtcNow.Date
                    .AddDays(-7));

        var request =
            new CreateFieldCollectionPaymentRequest
            {
                CollectionRouteId = routeId,
                LoanId = loan.Id,
                Amount = 100m,
                PaymentType =
                    PaymentType.Regular
            };

        var response =
            await context.Client.PostAsJsonAsync(
                "/api/field-collections/payments",
                request);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task CreatePayment_CollectorCannotUseRouteFromAnotherTenant()
    {
        var tenant1 = await CreateContextAsync();
        var tenant2 = await CreateContextAsync();

        var collector =
            await CreateCollectorAsync(
                tenant1);

        var collectorClient =
            await LoginCollectorAsync(
                tenant1.TenantId,
                collector);

        var routeId =
            await CreateRouteAsync(
                tenant2.Client);

        var investorId =
            await CreateInvestorAsync(
                tenant2.Client);

        var clientId =
            await CreateClientAsync(
                tenant2.Client,
                routeId,
                "Other",
                "Tenant");

        var loan =
            await CreateLoanAsync(
                tenant2.Client,
                investorId,
                clientId,
                DateTime.UtcNow.Date
                    .AddDays(-7));

        var request =
            new CreateFieldCollectionPaymentRequest
            {
                CollectionRouteId = routeId,
                LoanId = loan.Id,
                Amount = 100m,
                PaymentType =
                    PaymentType.Regular
            };

        var response =
            await collectorClient.PostAsJsonAsync(
                "/api/field-collections/payments",
                request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task CreatePayment_WithLateFee_AppliesLateFeeFirstAndReturnsCorrectReceipt()
    {
        /*
        * Weekly:
        *
        * StartDate = hoy - 8
        * DueDate   = hoy - 1
        * Mora      = efectiva hoy
        *
        * Contractual balance = 1300
        * Late fee            =   20
        * Total outstanding   = 1320
        */
        var setup =
            await CreatePaymentScenarioAsync(
                lateFeeEnabled: true,
                lateFeeAmount: 20m,
                startDate:
                    DateTime.UtcNow.Date
                        .AddDays(-8));

        var originalNextPaymentDate =
            setup.Loan.NextPaymentDate;

        /*
        * El cliente entrega:
        *
        * RD$20  -> mora
        * RD$100 -> préstamo
        */
        var request =
            CreatePaymentRequest(
                setup,
                120m,
                PaymentType.Regular,
                "Pago de cuota y mora");

        var response =
            await setup.CollectorClient
                .PostAsJsonAsync(
                    "/api/field-collections/payments",
                    request);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var receipt =
            await response.Content
                .ReadFromJsonAsync<
                    FieldCollectionPaymentResponse>();

        Assert.NotNull(receipt);

        Assert.Equal(
            120m,
            receipt.Amount);

        Assert.Equal(
            20m,
            receipt.AppliedToLateFees);

        Assert.Equal(
            100m,
            receipt.AppliedToLoan);

        Assert.Equal(
            1300m,
            receipt.BalanceBefore);

        Assert.Equal(
            1200m,
            receipt.BalanceAfter);

        Assert.Equal(
            20m,
            receipt.LateFeeBalanceBefore);

        Assert.Equal(
            0m,
            receipt.LateFeeBalanceAfter);

        Assert.Equal(
            1320m,
            receipt.TotalOutstandingBefore);

        Assert.Equal(
            1200m,
            receipt.TotalOutstandingAfter);

        Assert.Equal(
            originalNextPaymentDate.AddDays(7),
            receipt.NextPaymentDate);

        Assert.Equal(
            "Pago de cuota y mora",
            receipt.Notes);
    }
    [Fact]
    public async Task CreatePayment_PayingOnlyLateFee_DoesNotAdvanceLoanSchedule()
    {
        var setup =
            await CreatePaymentScenarioAsync(
                lateFeeEnabled: true,
                lateFeeAmount: 20m,
                startDate:
                    DateTime.UtcNow.Date
                        .AddDays(-8));

        var originalNextPaymentDate =
            setup.Loan.NextPaymentDate;

        var request =
            CreatePaymentRequest(
                setup,
                20m,
                PaymentType.Partial,
                "Pago solamente de mora");

        var response =
            await setup.CollectorClient
                .PostAsJsonAsync(
                    "/api/field-collections/payments",
                    request);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var receipt =
            await response.Content
                .ReadFromJsonAsync<
                    FieldCollectionPaymentResponse>();

        Assert.NotNull(receipt);

        Assert.Equal(
            20m,
            receipt.Amount);

        Assert.Equal(
            20m,
            receipt.AppliedToLateFees);

        Assert.Equal(
            0m,
            receipt.AppliedToLoan);

        Assert.Equal(
            1300m,
            receipt.BalanceBefore);

        Assert.Equal(
            1300m,
            receipt.BalanceAfter);

        Assert.Equal(
            20m,
            receipt.LateFeeBalanceBefore);

        Assert.Equal(
            0m,
            receipt.LateFeeBalanceAfter);

        Assert.Equal(
            1320m,
            receipt.TotalOutstandingBefore);

        Assert.Equal(
            1300m,
            receipt.TotalOutstandingAfter);

        /*
        * Como absolutamente nada fue aplicado al contrato,
        * la fecha de la próxima cuota debe permanecer igual.
        */
        Assert.Equal(
            originalNextPaymentDate,
            receipt.NextPaymentDate);
    }

    [Fact]
    public async Task CreatePayment_CreatesPersistentReceiptWithCollectorSnapshot()
    {
        var setup =
            await CreatePaymentScenarioAsync();

        var request =
            CreatePaymentRequest(
                setup,
                100m,
                PaymentType.Regular,
                "Pago con recibo persistente");

        var response =
            await setup.CollectorClient
                .PostAsJsonAsync(
                    "/api/field-collections/payments",
                    request);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var fieldPayment =
            await response.Content
                .ReadFromJsonAsync<
                    FieldCollectionPaymentResponse>();

        Assert.NotNull(fieldPayment);

        /*
        * Consultamos el PaymentReceipt usando
        * directamente el token del Collector.
        *
        * Esto valida también autorización para
        * reimpresión desde la aplicación móvil.
        */
        var receiptResponse =
            await setup.CollectorClient.GetAsync(
                $"/api/payments/{fieldPayment.PaymentId}/receipt");

        Assert.Equal(
            HttpStatusCode.OK,
            receiptResponse.StatusCode);

        var receipt =
            await receiptResponse.Content
                .ReadFromJsonAsync<
                    PaymentReceiptResponse>();

        Assert.NotNull(receipt);

        Assert.Equal(
            fieldPayment.PaymentId,
            receipt.PaymentId);

        Assert.Equal(
            setup.Loan.Id,
            receipt.LoanId);

        Assert.Equal(
            setup.ClientId,
            receipt.ClientId);

        Assert.Equal(
            100m,
            receipt.AmountReceived);

        Assert.Equal(
            0m,
            receipt.LateFeeAmountApplied);

        Assert.Equal(
            100m,
            receipt.LoanBalanceAmountApplied);

        Assert.Equal(
            1200m,
            receipt.ContractualBalanceAfter);

        Assert.Equal(
            1200m,
            receipt.TotalOutstandingAfter);

        Assert.Equal(
            setup.Collector.Id,
            receipt.CollectedByAppUserId);

        Assert.False(
            string.IsNullOrWhiteSpace(
                receipt.CollectedByName));

        Assert.Equal(
            "Pago con recibo persistente",
            receipt.Notes);
    }

    // ============================================================
    // SCENARIO
    // ============================================================

    private async Task<PaymentScenario>
        CreatePaymentScenarioAsync(
            bool lateFeeEnabled = false,
            decimal lateFeeAmount = 0m,
            int lateFeeGraceDays = 0,
            DateTime? startDate = null)
    {
        var context =
            await CreateContextAsync();

        var routeId =
            await CreateRouteAsync(
                context.Client);

        var collector =
            await CreateCollectorAsync(
                context);

        await AssignRouteAsync(
            context.Client,
            collector.Id,
            routeId);

        var investorId =
            await CreateInvestorAsync(
                context.Client);

        var clientId =
            await CreateClientAsync(
                context.Client,
                routeId,
                "Payment",
                "Client");

        var loan =
            await CreateLoanAsync(
                context.Client,
                investorId,
                clientId,
                startDate ??
                    DateTime.UtcNow.Date
                        .AddDays(-7),
                lateFeeEnabled,
                lateFeeAmount,
                lateFeeGraceDays);

        var collectorClient =
            await LoginCollectorAsync(
                context.TenantId,
                collector);

        return new PaymentScenario(
            context.TenantId,
            routeId,
            collector,
            clientId,
            loan,
            context.Client,
            collectorClient);
    }

    private sealed record PaymentScenario(
        Guid TenantId,
        Guid RouteId,
        CollectorCredentials Collector,
        Guid ClientId,
        LoanResponse Loan,
        HttpClient AdministratorClient,
        HttpClient CollectorClient);

    private sealed record CollectorCredentials(
        Guid Id,
        string Username,
        string Password);

    // ============================================================
    // AUTHENTICATION HELPERS
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
            $"field-{suffix}";

        var password =
            TestAuthenticationHelper
                .DefaultPassword;

        var request = new
        {
            name =
                $"Field Collector {suffix}",
            username,
            password,
            email =
                $"field-user-{suffix}@example.com",
            phone =
                "8095551234",
            role =
                (int)AppUserRole.Collector
        };

        var response =
            await context.Client.PostAsJsonAsync(
                "/api/app-users",
                request);

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
        var loginClient =
            _factory.CreateClient();

        var response =
            await loginClient.PostAsJsonAsync(
                "/api/auth/login",
                new LoginRequest
                {
                    TenantId = tenantId,
                    Username =
                        collector.Username,
                    Password =
                        collector.Password
                });

        response.EnsureSuccessStatusCode();

        var auth =
            await response.Content
                .ReadFromJsonAsync<AuthResponse>();

        Assert.NotNull(auth);

        Assert.False(
            string.IsNullOrWhiteSpace(
                auth.AccessToken));

        var client =
            _factory.CreateClient();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                auth.AccessToken);

        return client;
    }

    // ============================================================
    // API HELPERS
    // ============================================================

    private static async Task<HttpResponseMessage>
        GetDailyAsync(
            HttpClient collectorClient,
            DateOnly date)
    {
        return await collectorClient.GetAsync(
            $"/api/field-collections/daily" +
            $"?date={date:yyyy-MM-dd}");
    }

    private static async Task<Guid>
        CreateRouteAsync(
            HttpClient administratorClient)
    {
        var request =
            new CreateCollectionRouteRequest
            {
                Name =
                    $"Field Route {Guid.NewGuid():N}",
                Description =
                    "Field collection integration test"
            };

        var response =
            await administratorClient.PostAsJsonAsync(
                "/api/collection-routes",
                request);

        response.EnsureSuccessStatusCode();

        var route =
            await response.Content
                .ReadFromJsonAsync<
                    CollectionRouteResponse>();

        Assert.NotNull(route);

        return route.Id;
    }

    private static async Task
        AssignRouteAsync(
            HttpClient administratorClient,
            Guid collectorId,
            Guid routeId)
    {
        var response =
            await administratorClient.PostAsync(
                $"/api/app-users/{collectorId}" +
                $"/collection-routes/{routeId}",
                null);

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);
    }

    private static async Task
        CreateScheduleAsync(
            HttpClient administratorClient,
            Guid routeId,
            CollectionWeekDay day)
    {
        var request =
            new CreateCollectionRouteScheduleRequest
            {
                CollectionRouteId =
                    routeId,
                DayOfWeek = day
            };

        var response =
            await administratorClient.PostAsJsonAsync(
                "/api/collection-route-schedules",
                request);

        response.EnsureSuccessStatusCode();
    }

    private static async Task<Guid>
        CreateInvestorAsync(
            HttpClient administratorClient)
    {
        var request =
            new CreateInvestorRequest
            {
                Name =
                    $"Field Investor {Guid.NewGuid():N}",
                Phone =
                    "8095551000",
                Identification =
                    $"INV-{Guid.NewGuid():N}"
            };

        var response =
            await administratorClient.PostAsJsonAsync(
                "/api/investors",
                request);

        response.EnsureSuccessStatusCode();

        var investor =
            await response.Content
                .ReadFromJsonAsync<
                    InvestorResponse>();

        Assert.NotNull(investor);

        return investor.Id;
    }

    private static async Task<Guid>
        CreateClientAsync(
            HttpClient administratorClient,
            Guid routeId,
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
                        9999999)}",
                Address =
                    "Field Collection Test Address",
                Latitude = 19.45m,
                Longitude = -70.69m,
                CollectionRouteId =
                    routeId
            };

        var response =
            await administratorClient.PostAsJsonAsync(
                "/api/clients",
                request);

        response.EnsureSuccessStatusCode();

        var client =
            await response.Content
                .ReadFromJsonAsync<
                    ClientResponse>();

        Assert.NotNull(client);

        return client.Id;
    }

    private static async Task<LoanResponse>
    CreateLoanAsync(
        HttpClient administratorClient,
        Guid investorId,
        Guid clientId,
        DateTime startDate,
        bool lateFeeEnabled = false,
        decimal lateFeeAmount = 0m,
        int lateFeeGraceDays = 0)
    {
        var request =
            new CreateLoanRequest
            {
                InvestorId = investorId,
                ClientId = clientId,
                PrincipalAmount = 1000m,
                InstallmentAmount = 100m,
                TotalInstallments = 13,
                PaymentFrequency =
                    PaymentFrequency.Weekly,
                LateFeeEnabled =
                    lateFeeEnabled,

                LateFeeCalculationType =
                    LateFeeCalculationType
                        .FixedAmountPerInstallment,

                LateFeeAmount =
                    lateFeeAmount,

                LateFeeGraceDays =
                    lateFeeGraceDays,
                StartDate = startDate,
                Notes =
                    "Field collection integration test"
            };

        var response =
            await administratorClient.PostAsJsonAsync(
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

    private static async Task
        SetManualOrderAsync(
            HttpClient administratorClient,
            Guid routeId,
            IReadOnlyCollection<Guid> clientIds)
    {
        var request = new
        {
            clientIds
        };

        var response =
            await administratorClient.PutAsJsonAsync(
                $"/api/collection-routes/{routeId}" +
                "/clients/order",
                request);

        response.EnsureSuccessStatusCode();
    }

    private static
        CreateFieldCollectionPaymentRequest
        CreatePaymentRequest(
            PaymentScenario setup,
            decimal amount,
            PaymentType paymentType,
            string? notes = null)
    {
        return new CreateFieldCollectionPaymentRequest
        {
            CollectionRouteId =
                setup.RouteId,
            LoanId =
                setup.Loan.Id,
            Amount =
                amount,
            PaymentType =
                paymentType,
            Notes =
                notes
        };
    }
}