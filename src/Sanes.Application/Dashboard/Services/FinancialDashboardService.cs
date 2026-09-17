using Sanes.Application.Dashboard.DTOs;
using Sanes.Application.Dashboard.Repositories;
using Sanes.Application.Loans.Services;

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

        var delinquencyRate = 0m;

        if (contractualBalanceOutstanding > 0)
        {
            delinquencyRate =
                Math.Round(
                    (
                        overdueContractualAmount /
                        contractualBalanceOutstanding
                    ) * 100m,
                    2,
                    MidpointRounding.AwayFromZero);
        }

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
}