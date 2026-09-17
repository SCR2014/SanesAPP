using Sanes.Application.Dashboard.DTOs;
using Sanes.Application.Dashboard.Repositories;
using Sanes.Application.Loans.Services;
using Sanes.Application.Loans.DTOs;

namespace Sanes.Application.Dashboard.Services;

public class FinancialDashboardService
    : IFinancialDashboardService
{
    private const int MaximumCashFlowDays = 366;

    private readonly IFinancialDashboardRepository
        _dashboardRepository;

    private readonly ILoanService
        _loanService;

    public FinancialDashboardService(
        IFinancialDashboardRepository dashboardRepository,
        ILoanService loanService)
    {
        _dashboardRepository =
            dashboardRepository;

        _loanService =
            loanService;
    }

    public async Task<FinancialDashboardSummaryResponse>
        GetSummaryAsync(
            Guid tenantId,
            CancellationToken cancellationToken = default)
    {
        ValidateTenantId(
            tenantId);

        /*
         * GetActivePortfolioAsync es la autoridad
         * existente sobre el estado financiero actual.
         *
         * Aquí se calculan actualmente:
         * - balance contractual,
         * - mora pendiente,
         * - total pendiente,
         * - vencimiento,
         * - monto contractual vencido.
         *
         * Además, antes de devolver la cartera,
         * materializa las moras que correspondan.
         */
        var portfolio =
            await _loanService
                .GetActivePortfolioAsync(
                    tenantId,
                    cancellationToken);

        /*
         * Estas son métricas históricas que no dependen
         * exclusivamente de préstamos actualmente activos.
         */
        var historical =
            await _dashboardRepository
                .GetHistoricalDataAsync(
                    tenantId,
                    cancellationToken);

        var activeLoansCount =
            portfolio.Count;

        var activeClientsCount =
            portfolio
                .Select(x => x.ClientId)
                .Distinct()
                .Count();

        var contractualBalanceOutstanding =
            portfolio.Sum(x =>
                x.Balance);

        var lateFeeBalanceOutstanding =
            portfolio.Sum(x =>
                x.LateFeeBalance);

        var totalOutstanding =
            portfolio.Sum(x =>
                x.TotalOutstanding);

        var overdueLoansCount =
            portfolio.Count(x =>
                x.IsOverdue);

        var overdueContractualAmount =
            portfolio.Sum(x =>
                x.OverdueAmount);

        var grossContractualInterest =
            historical.ContractualAmountOriginated -
            historical.PrincipalOriginated;

        if (grossContractualInterest < 0)
        {
            grossContractualInterest = 0;
        }

        var netContractualInterest =
            grossContractualInterest -
            historical.EarlySettlementDiscounts;

        if (netContractualInterest < 0)
        {
            netContractualInterest = 0;
        }

        var delinquencyRate =
            CalculateDelinquencyRate(
                overdueContractualAmount,
                contractualBalanceOutstanding);

        return new FinancialDashboardSummaryResponse
        {
            ActiveLoansCount =
                activeLoansCount,

            ActiveClientsCount =
                activeClientsCount,

            PrincipalOriginated =
                historical.PrincipalOriginated,

            ContractualAmountOriginated =
                historical.ContractualAmountOriginated,

            GrossContractualInterest =
                grossContractualInterest,

            EarlySettlementDiscounts =
                historical.EarlySettlementDiscounts,

            NetContractualInterest =
                netContractualInterest,

            ContractualBalanceOutstanding =
                contractualBalanceOutstanding,

            LateFeeBalanceOutstanding =
                lateFeeBalanceOutstanding,

            TotalOutstanding =
                totalOutstanding,

            CashCollected =
                historical.CashCollected,

            ContractualCashCollected =
                historical.ContractualCashCollected,

            LateFeesCollected =
                historical.LateFeesCollected,

            OverdueLoansCount =
                overdueLoansCount,

            OverdueContractualAmount =
                overdueContractualAmount,

            DelinquencyRate =
                delinquencyRate
        };
    }

    public async Task<FinancialDashboardCashFlowResponse>
        GetCashFlowAsync(
            Guid tenantId,
            DateOnly from,
            DateOnly to,
            CancellationToken cancellationToken = default)
    {
        ValidateTenantId(
            tenantId);

        ValidateCashFlowPeriod(
            from,
            to);

        var data =
            await _dashboardRepository
                .GetCashFlowAsync(
                    tenantId,
                    from,
                    to,
                    cancellationToken);

        /*
         * El repositorio solamente devuelve días
         * donde hubo alguna actividad.
         *
         * Para gráficas es más útil entregar una
         * serie continua, incluyendo días en cero.
         */
        var dataByDate =
            data.ToDictionary(
                x => x.Date);

        var items =
            new List<
                FinancialDashboardCashFlowItemResponse>();

        var currentDate =
            from;

        while (currentDate <= to)
        {
            dataByDate.TryGetValue(
                currentDate,
                out var day);

            items.Add(
                new FinancialDashboardCashFlowItemResponse
                {
                    Date =
                        currentDate,

                    PrincipalOriginated =
                        day?.PrincipalOriginated
                        ?? 0m,

                    CashCollected =
                        day?.CashCollected
                        ?? 0m,

                    ContractualCashCollected =
                        day?.ContractualCashCollected
                        ?? 0m,

                    LateFeesCollected =
                        day?.LateFeesCollected
                        ?? 0m,

                    EarlySettlementDiscounts =
                        day?.EarlySettlementDiscounts
                        ?? 0m
                });

            currentDate =
                currentDate.AddDays(1);
        }

        return new FinancialDashboardCashFlowResponse
        {
            From =
                from,

            To =
                to,

            PrincipalOriginated =
                items.Sum(x =>
                    x.PrincipalOriginated),

            CashCollected =
                items.Sum(x =>
                    x.CashCollected),

            ContractualCashCollected =
                items.Sum(x =>
                    x.ContractualCashCollected),

            LateFeesCollected =
                items.Sum(x =>
                    x.LateFeesCollected),

            EarlySettlementDiscounts =
                items.Sum(x =>
                    x.EarlySettlementDiscounts),

            Items =
                items
        };
    }

    public async Task<List<FinancialDashboardInvestorBreakdownResponse>>
        GetInvestorsAsync(
            Guid tenantId,
            CancellationToken cancellationToken = default)
    {
        ValidateTenantId(
            tenantId);

        /*
        * Una sola construcción de la cartera activa.
        *
        * Esto mantiene saldo, mora y vencimiento
        * exactamente alineados con LoanService.
        */
        var portfolio =
            await _loanService
                .GetActivePortfolioAsync(
                    tenantId,
                    cancellationToken);

        var historicalData =
            await _dashboardRepository
                .GetInvestorHistoricalDataAsync(
                    tenantId,
                    cancellationToken);

        var portfolioByInvestor =
            portfolio
                .GroupBy(x =>
                    x.InvestorId)
                .ToDictionary(
                    x => x.Key,
                    x => x.ToList());

        var result =
            new List<
                FinancialDashboardInvestorBreakdownResponse>();

        foreach (var historical in historicalData)
        {
            portfolioByInvestor.TryGetValue(
                historical.InvestorId,
                out var investorPortfolio);

            investorPortfolio ??=
                new List<
                    ActiveLoanPortfolioItemResponse>();

            var hasCurrentPortfolio =
                investorPortfolio.Count > 0;

            var hasHistoricalActivity =
                historical.PrincipalOriginated != 0 ||
                historical.ContractualAmountOriginated != 0 ||
                historical.EarlySettlementDiscounts != 0 ||
                historical.CashCollected != 0 ||
                historical.ContractualCashCollected != 0 ||
                historical.LateFeesCollected != 0;

            /*
            * Activos siempre aparecen.
            *
            * Inactivos solamente si conservan
            * historial o cartera actual.
            */
            if (
                !historical.IsActive &&
                !hasCurrentPortfolio &&
                !hasHistoricalActivity)
            {
                continue;
            }

            var contractualBalanceOutstanding =
                investorPortfolio.Sum(x =>
                    x.Balance);

            var lateFeeBalanceOutstanding =
                investorPortfolio.Sum(x =>
                    x.LateFeeBalance);

            var totalOutstanding =
                investorPortfolio.Sum(x =>
                    x.TotalOutstanding);

            var overdueContractualAmount =
                investorPortfolio.Sum(x =>
                    x.OverdueAmount);

            var grossContractualInterest =
                historical.ContractualAmountOriginated -
                historical.PrincipalOriginated;

            if (grossContractualInterest < 0)
            {
                grossContractualInterest = 0;
            }

            var netContractualInterest =
                grossContractualInterest -
                historical.EarlySettlementDiscounts;

            if (netContractualInterest < 0)
            {
                netContractualInterest = 0;
            }

            result.Add(
                new FinancialDashboardInvestorBreakdownResponse
                {
                    InvestorId =
                        historical.InvestorId,

                    InvestorName =
                        historical.InvestorName,

                    IsActive =
                        historical.IsActive,

                    ActiveLoansCount =
                        investorPortfolio.Count,

                    ActiveClientsCount =
                        investorPortfolio
                            .Select(x =>
                                x.ClientId)
                            .Distinct()
                            .Count(),

                    PrincipalOriginated =
                        historical.PrincipalOriginated,

                    ContractualAmountOriginated =
                        historical.ContractualAmountOriginated,

                    GrossContractualInterest =
                        grossContractualInterest,

                    EarlySettlementDiscounts =
                        historical.EarlySettlementDiscounts,

                    NetContractualInterest =
                        netContractualInterest,

                    ContractualBalanceOutstanding =
                        contractualBalanceOutstanding,

                    LateFeeBalanceOutstanding =
                        lateFeeBalanceOutstanding,

                    TotalOutstanding =
                        totalOutstanding,

                    CashCollected =
                        historical.CashCollected,

                    ContractualCashCollected =
                        historical.ContractualCashCollected,

                    LateFeesCollected =
                        historical.LateFeesCollected,

                    OverdueLoansCount =
                        investorPortfolio.Count(x =>
                            x.IsOverdue),

                    OverdueContractualAmount =
                        overdueContractualAmount,

                    DelinquencyRate =
                        CalculateDelinquencyRate(
                            overdueContractualAmount,
                            contractualBalanceOutstanding)
                });
        }

        return result
            .OrderByDescending(x =>
                x.IsActive)
            .ThenBy(x =>
                x.InvestorName)
            .ToList();
    }

    public async Task<List<FinancialDashboardRouteBreakdownResponse>>
        GetRoutesAsync(
            Guid tenantId,
            CancellationToken cancellationToken = default)
    {
        ValidateTenantId(
            tenantId);

        /*
        * De nuevo construimos la cartera activa
        * una sola vez.
        */
        var portfolio =
            await _loanService
                .GetActivePortfolioAsync(
                    tenantId,
                    cancellationToken);

        var routes =
            await _dashboardRepository
                .GetRoutesAsync(
                    tenantId,
                    cancellationToken);

        var clientRoutes =
            await _dashboardRepository
                .GetClientRoutesAsync(
                    tenantId,
                    cancellationToken);

        var routeByClient =
            clientRoutes.ToDictionary(
                x => x.ClientId,
                x => x.CollectionRouteId);

        /*
        * Enriquecemos cada préstamo solamente con
        * la ruta ACTUAL de su cliente.
        *
        * Esto es una fotografía operacional,
        * no una atribución histórica.
        */
        var routedPortfolio =
            portfolio
                .Select(x =>
                {
                    Guid? collectionRouteId =
                        null;

                    if (
                        routeByClient.TryGetValue(
                            x.ClientId,
                            out var clientRouteId))
                    {
                        collectionRouteId =
                            clientRouteId;
                    }

                    return new
                    {
                        Loan =
                            x,

                        CollectionRouteId =
                            collectionRouteId
                    };
                })
                .ToList();

        var assignedByRoute =
            routedPortfolio
                .Where(x =>
                    x.CollectionRouteId.HasValue)
                .GroupBy(x =>
                    x.CollectionRouteId!.Value)
                .ToDictionary(
                    x => x.Key,
                    x => x
                        .Select(item =>
                            item.Loan)
                        .ToList());

        var unassignedPortfolio =
            routedPortfolio
                .Where(x =>
                    !x.CollectionRouteId.HasValue)
                .Select(x =>
                    x.Loan)
                .ToList();

        var result =
            new List<
                FinancialDashboardRouteBreakdownResponse>();

        foreach (var route in routes)
        {
            assignedByRoute.TryGetValue(
                route.CollectionRouteId,
                out var routePortfolio);

            routePortfolio ??=
                new List<
                    ActiveLoanPortfolioItemResponse>();

            /*
            * Rutas activas aparecen siempre.
            *
            * Una ruta inactiva solamente continúa
            * visible si todavía conserva cartera activa.
            */
            if (
                !route.IsActive &&
                routePortfolio.Count == 0)
            {
                continue;
            }

            result.Add(
                BuildRouteBreakdown(
                    route.CollectionRouteId,
                    route.CollectionRouteName,
                    route.IsActive,
                    false,
                    routePortfolio));
        }

        /*
        * Los préstamos activos cuyos clientes no tengan
        * ruta deben seguir reconciliando con el dashboard
        * global.
        */
        if (unassignedPortfolio.Count > 0)
        {
            result.Add(
                BuildRouteBreakdown(
                    null,
                    "Sin ruta",
                    false,
                    true,
                    unassignedPortfolio));
        }

        return result
            .OrderBy(x =>
                x.IsUnassigned)
            .ThenByDescending(x =>
                x.IsActive)
            .ThenBy(x =>
                x.CollectionRouteName)
            .ToList();
    }

    private static void ValidateTenantId(
        Guid tenantId)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException(
                "TenantId must be a valid identifier.");
        }
    }

    private static void ValidateCashFlowPeriod(
        DateOnly from,
        DateOnly to)
    {
        if (from > to)
        {
            throw new ArgumentException(
                "'from' cannot be later than 'to'.");
        }

        var days =
            to.DayNumber -
            from.DayNumber +
            1;

        if (days > MaximumCashFlowDays)
        {
            throw new ArgumentException(
                $"Cash flow period cannot exceed {MaximumCashFlowDays} days.");
        }
    }

    private static FinancialDashboardRouteBreakdownResponse
        BuildRouteBreakdown(
            Guid? collectionRouteId,
            string collectionRouteName,
            bool isActive,
            bool isUnassigned,
            List<ActiveLoanPortfolioItemResponse> portfolio)
    {
        var contractualBalanceOutstanding =
            portfolio.Sum(x =>
                x.Balance);

        var lateFeeBalanceOutstanding =
            portfolio.Sum(x =>
                x.LateFeeBalance);

        var totalOutstanding =
            portfolio.Sum(x =>
                x.TotalOutstanding);

        var nextInstallmentAmountDue =
            portfolio.Sum(x =>
                x.NextInstallmentAmountDue);

        /*
        * Mantiene el significado operativo ya usado:
        *
        * próxima cuota contractual pendiente
        * + mora pendiente.
        */
        var collectionAmountDue =
            nextInstallmentAmountDue +
            lateFeeBalanceOutstanding;

        var overdueContractualAmount =
            portfolio.Sum(x =>
                x.OverdueAmount);

        /*
        * Utilizamos el mismo valor calculado por
        * LoanService para cada préstamo:
        *
        * overdue contractual + mora.
        */
        var totalOverdueAmountDue =
            portfolio.Sum(x =>
                x.TotalOverdueAmountDue);

        return new FinancialDashboardRouteBreakdownResponse
        {
            CollectionRouteId =
                collectionRouteId,

            CollectionRouteName =
                collectionRouteName,

            IsActive =
                isActive,

            IsUnassigned =
                isUnassigned,

            ActiveLoansCount =
                portfolio.Count,

            ActiveClientsCount =
                portfolio
                    .Select(x =>
                        x.ClientId)
                    .Distinct()
                    .Count(),

            ContractualBalanceOutstanding =
                contractualBalanceOutstanding,

            LateFeeBalanceOutstanding =
                lateFeeBalanceOutstanding,

            TotalOutstanding =
                totalOutstanding,

            NextInstallmentAmountDue =
                nextInstallmentAmountDue,

            CollectionAmountDue =
                collectionAmountDue,

            OverdueLoansCount =
                portfolio.Count(x =>
                    x.IsOverdue),

            OverdueContractualAmount =
                overdueContractualAmount,

            TotalOverdueAmountDue =
                totalOverdueAmountDue,

            DelinquencyRate =
                CalculateDelinquencyRate(
                    overdueContractualAmount,
                    contractualBalanceOutstanding)
        };
    }

    private static decimal CalculateDelinquencyRate(
        decimal overdueContractualAmount,
        decimal contractualBalanceOutstanding)
    {
        if (contractualBalanceOutstanding <= 0)
        {
            return 0m;
        }

        return Math.Round(
            (
                overdueContractualAmount /
                contractualBalanceOutstanding
            ) * 100m,
            2,
            MidpointRounding.AwayFromZero);
    }
}