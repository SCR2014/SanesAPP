using Sanes.Application.AppUsers.Repositories;
using Sanes.Application.CollectionAgenda.Services;
using Sanes.Application.FieldCollections.DTOs;
using Sanes.Application.Loans.Services;
using Sanes.Application.Payments.DTOs;
using Sanes.Application.Payments.Services;
using Sanes.Application.Clients.Repositories;
using Sanes.Application.CollectionRoutes.Repositories;
using Sanes.Domain.Enums;

namespace Sanes.Application.FieldCollections.Services;

public class FieldCollectionService : IFieldCollectionService
{
    private readonly IAppUserRepository _appUserRepository;
    private readonly ICollectionAgendaService _collectionAgendaService;
    private readonly ILoanService _loanService;
    private readonly IPaymentService _paymentService;
    private readonly IClientRepository _clientRepository;
    private readonly ICollectionRouteRepository _collectionRouteRepository; 

    public FieldCollectionService(
        IAppUserRepository appUserRepository,
        ICollectionAgendaService collectionAgendaService,
        ILoanService loanService,
        IPaymentService paymentService,
        IClientRepository clientRepository,
        ICollectionRouteRepository collectionRouteRepository)
    {
        _appUserRepository = appUserRepository;
        _collectionAgendaService = collectionAgendaService;
        _loanService = loanService;
        _paymentService = paymentService;
        _clientRepository = clientRepository;
        _collectionRouteRepository = collectionRouteRepository;
    }

    public async Task<FieldCollectionDailyResponse> GetDailyAsync(
        Guid tenantId,
        Guid appUserId,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException(
                "TenantId must be a valid identifier.");
        }

        if (appUserId == Guid.Empty)
        {
            throw new ArgumentException(
                "AppUserId must be a valid identifier.");
        }

        var collector =
            await _appUserRepository.GetByIdAsync(
                appUserId,
                tenantId,
                cancellationToken);

        if (collector is null)
        {
            throw new ArgumentException(
                "App user does not exist, is inactive, or does not belong to this tenant.");
        }

        if (collector.Role != AppUserRole.Collector)
        {
            throw new ArgumentException(
                "The selected app user is not a collector.");
        }

        var agenda =
            await _collectionAgendaService.GetDailyAgendaAsync(
                tenantId,
                date,
                appUserId,
                cancellationToken);

        var routes =
            new List<FieldCollectionRouteResponse>();

        /*
         * Collection Portfolio trabaja con DateTime.
         * La fecha se trata como fecha de negocio y no como
         * un instante horario.
         */
        var collectionDate =
            DateTime.SpecifyKind(
                date.ToDateTime(TimeOnly.MinValue),
                DateTimeKind.Utc);

        var collectionLoans =
            await _loanService.GetCollectionPortfolioAsync(
                tenantId: tenantId,
                collectionDate: collectionDate,
                cancellationToken: cancellationToken);
        /*
             * Indexamos los préstamos por cliente para no
             * recorrer toda la cartera por cada cliente.
             */
        var loansByClient =
            collectionLoans
                .GroupBy(x => x.ClientId)
                .ToDictionary(
                    x => x.Key,
                    x => x.ToList());


        foreach (var agendaRoute in agenda.Routes)
        {
            var clients =
                new List<FieldCollectionClientResponse>();

            foreach (var agendaClient in agendaRoute.Clients)
            {
                if (!loansByClient.TryGetValue(
                        agendaClient.ClientId,
                        out var clientLoans))
                {
                    /*
                     * Si el cliente pertenece a la ruta pero
                     * hoy no tiene ningún préstamo pendiente
                     * de cobro, no aparece en la cobranza
                     * operativa.
                     */
                    continue;
                }

                var loans =
                    clientLoans
                        .Select(x =>
                            new FieldCollectionLoanResponse
                            {
                                LoanId =
                                    x.LoanId,

                                Balance =
                                    x.Balance,

                                InstallmentAmount =
                                    x.InstallmentAmount,

                                NextInstallmentAmountDue =
                                    x.NextInstallmentAmountDue,

                                NextPaymentDate =
                                    x.NextPaymentDate,

                                OverdueAmount =
                                    x.OverdueAmount,

                                DaysOverdue =
                                    x.DaysOverdue,

                                OverdueInstallments =
                                    x.OverdueInstallments,

                                IsOverdue =
                                    x.IsOverdue
                            })
                        .ToList();

                clients.Add(
                    new FieldCollectionClientResponse
                    {
                        ClientId =
                            agendaClient.ClientId,

                        FirstName =
                            agendaClient.FirstName,

                        LastName =
                            agendaClient.LastName,

                        Phone =
                            agendaClient.Phone,

                        Address =
                            agendaClient.Address,

                        CollectionRouteOrder =
                            agendaClient.CollectionRouteOrder,

                        Loans =
                            loans,

                        Latitude =
                            agendaClient.Latitude,

                        Longitude =
                            agendaClient.Longitude
                    });
            }

            /*
             * Una ruta sigue apareciendo aunque ninguno de
             * sus clientes tenga un préstamo para cobrar hoy.
             * Sigue siendo una ruta programada del cobrador.
             */
            routes.Add(
                new FieldCollectionRouteResponse
                {
                    CollectionRouteId =
                        agendaRoute.CollectionRouteId,

                    CollectionRouteName =
                        agendaRoute.CollectionRouteName,

                    StartTime =
                        agendaRoute.StartTime,

                    EndTime =
                        agendaRoute.EndTime,

                    ClientsCount =
                        clients.Count,

                    LoansCount =
                        clients.Sum(x => x.Loans.Count),

                    TotalBalance =
                        clients
                            .SelectMany(x => x.Loans)
                            .Sum(x => x.Balance),

                    TotalOverdueAmount =
                        clients
                            .SelectMany(x => x.Loans)
                            .Sum(x => x.OverdueAmount),

                    TotalAmountDue =
                        clients
                            .SelectMany(x => x.Loans)
                            .Sum(x => x.NextInstallmentAmountDue),

                    Clients =
                        clients
                });
        }

        return new FieldCollectionDailyResponse
        {
            Date =
                date,

            AppUserId =
                collector.Id,

            CollectorName =
                collector.Name,

            RoutesCount =
                routes.Count,

            ClientsCount =
                routes.Sum(x => x.ClientsCount),

            LoansCount =
                routes.Sum(x => x.LoansCount),

            TotalBalance =
                routes.Sum(x => x.TotalBalance),

            TotalOverdueAmount =
                routes.Sum(x => x.TotalOverdueAmount),
            TotalAmountDue =
                routes.Sum(x => x.TotalAmountDue),

            Routes =
                routes
        };
    }
    public async Task<FieldCollectionPaymentResponse> CreatePaymentAsync(
        Guid tenantId,
        Guid appUserId,
        CreateFieldCollectionPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        /*
        * PaymentService mantiene toda la autoridad sobre
        * las reglas financieras y las validaciones del
        * contexto operacional.
        */
        var payment =
            await _paymentService.CreateAsync(
                tenantId,
                new CreatePaymentRequest
                {
                    LoanId =
                        request.LoanId,

                    CollectedByAppUserId =
                        appUserId,

                    CollectionRouteId =
                        request.CollectionRouteId,

                    Amount =
                        request.Amount,

                    PaymentDate =
                        DateTime.UtcNow,

                    PaymentType =
                        request.PaymentType,

                    Notes =
                        request.Notes
                },
                cancellationToken);

        var financialSummary =
            await _loanService.GetFinancialSummaryAsync(
                tenantId,
                request.LoanId,
                cancellationToken);

        if (financialSummary is null)
        {
            throw new InvalidOperationException(
                "Unable to retrieve the loan financial summary after payment.");
        }

        var collector =
            await _appUserRepository.GetByIdAsync(
                appUserId,
                tenantId,
                cancellationToken);

        var route =
            await _collectionRouteRepository.GetByIdAsync(
                request.CollectionRouteId,
                tenantId,
                cancellationToken);

        var loan =
            await _loanService.GetByIdAsync(
                tenantId,
                request.LoanId,
                cancellationToken);

        if (collector is null ||
            route is null ||
            loan is null)
        {
            throw new InvalidOperationException(
                "Unable to build the field collection receipt.");
        }

        var client =
            await _clientRepository.GetByIdAsync(
                tenantId,
                loan.ClientId,
                cancellationToken);

        if (client is null)
        {
            throw new InvalidOperationException(
                "Unable to retrieve the client after payment.");
        }

        var clientName =
            string.Join(
                " ",
                new[]
                {
                    client.FirstName,
                    client.LastName
                }
                .Where(x =>
                    !string.IsNullOrWhiteSpace(x)));

        return new FieldCollectionPaymentResponse
        {
            PaymentId =
                payment.Id,

            PaymentDate =
                payment.PaymentDate,

            AppUserId =
                collector.Id,

            CollectorName =
                collector.Name,

            CollectionRouteId =
                route.Id,

            CollectionRouteName =
                route.Name,

            ClientId =
                client.Id,

            ClientName =
                clientName,

            ClientPhone =
                client.Phone,

            LoanId =
                loan.Id,

            Amount =
                payment.Amount,

            PaymentType =
                payment.PaymentType,

            BalanceBefore =
                financialSummary.Balance + payment.Amount,

            BalanceAfter =
                financialSummary.Balance,

            NextInstallmentAmountDue =
                financialSummary.NextInstallmentAmountDue,

            NextPaymentDate =
                financialSummary.Status == LoanStatus.Paid
                    ? null
                    : financialSummary.NextPaymentDate,

            Notes =
                payment.Notes
        };
    }
}