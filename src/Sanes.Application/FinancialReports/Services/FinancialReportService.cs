using Sanes.Application.FinancialReports.DTOs;
using Sanes.Application.FinancialReports.Repositories;
using Sanes.Application.Loans.DTOs;
using Sanes.Application.Loans.Services;
using Sanes.Application.FinancialReports.Models;
using Sanes.Application.Dashboard.Repositories;

namespace Sanes.Application.FinancialReports.Services;

public class FinancialReportService
    : IFinancialReportService
{
    private readonly IFinancialReportRepository
        _financialReportRepository;

    private readonly ILoanService
        _loanService;

    private readonly IFinancialDashboardRepository
    _financialDashboardRepository;

    public FinancialReportService(
        IFinancialReportRepository financialReportRepository,
        IFinancialDashboardRepository financialDashboardRepository,
        ILoanService loanService)
    {
        _financialReportRepository =
            financialReportRepository;

        _financialDashboardRepository =
            financialDashboardRepository;

        _loanService =
            loanService;
    }

    public async Task<FinancialPortfolioReportResponse>
        GetPortfolioAsync(
            Guid tenantId,
            Guid? investorId = null,
            Guid? collectionRouteId = null,
            bool overdueOnly = false,
            CancellationToken cancellationToken = default)
    {
        ValidateIdentifiers(
            tenantId,
            investorId,
            collectionRouteId);

        /*
         * LoanService continúa siendo la autoridad
         * sobre:
         *
         * - saldo contractual
         * - mora
         * - vencimiento
         * - cuotas vencidas
         * - próximo pago
         */
        var portfolio =
            await _loanService
                .GetActivePortfolioAsync(
                    tenantId,
                    cancellationToken);

        var investors =
            await _financialReportRepository
                .GetInvestorsAsync(
                    tenantId,
                    cancellationToken);

        var clientRoutes =
            await _financialReportRepository
                .GetClientRoutesAsync(
                    tenantId,
                    cancellationToken);

        var investorNames =
            investors.ToDictionary(
                x => x.InvestorId,
                x => x.InvestorName);

        var routeByClient =
            clientRoutes.ToDictionary(
                x => x.ClientId);

        IEnumerable<ActiveLoanPortfolioItemResponse>
            filteredPortfolio =
                portfolio;

        if (investorId.HasValue)
        {
            filteredPortfolio =
                filteredPortfolio.Where(x =>
                    x.InvestorId ==
                    investorId.Value);
        }

        if (collectionRouteId.HasValue)
        {
            filteredPortfolio =
                filteredPortfolio.Where(x =>
                    routeByClient.TryGetValue(
                        x.ClientId,
                        out var clientRoute) &&
                    clientRoute.CollectionRouteId ==
                    collectionRouteId.Value);
        }

        if (overdueOnly)
        {
            /*
             * Misma semántica operacional utilizada
             * por la cartera de cobranza existente.
             */
            filteredPortfolio =
                filteredPortfolio.Where(x =>
                    x.IsOverdue ||
                    x.HasOutstandingLateFees);
        }

        var items =
            filteredPortfolio
                .Select(x =>
                    MapPortfolioItem(
                        x,
                        investorNames,
                        routeByClient))
                .OrderByDescending(x =>
                    x.IsOverdue ||
                    x.HasOutstandingLateFees)
                .ThenBy(x =>
                    x.NextPaymentDate)
                .ThenBy(x =>
                    x.ClientName)
                .ToList();

        return new FinancialPortfolioReportResponse
        {
            Summary =
                BuildPortfolioSummary(
                    items),

            Items =
                items
        };
    }

    public async Task<FinancialDelinquencyReportResponse>
        GetDelinquencyAsync(
            Guid tenantId,
            Guid? investorId = null,
            Guid? collectionRouteId = null,
            CancellationToken cancellationToken = default)
    {
        ValidateIdentifiers(
            tenantId,
            investorId,
            collectionRouteId);

        /*
         * Importante:
         *
         * Para calcular la tasa de morosidad cargamos
         * primero toda la cartera correspondiente al
         * filtro.
         *
         * Luego extraemos solamente los préstamos
         * vencidos / con mora para Items y Aging.
         */
        var portfolio =
            await _loanService
                .GetActivePortfolioAsync(
                    tenantId,
                    cancellationToken);

        var investors =
            await _financialReportRepository
                .GetInvestorsAsync(
                    tenantId,
                    cancellationToken);

        var clientRoutes =
            await _financialReportRepository
                .GetClientRoutesAsync(
                    tenantId,
                    cancellationToken);

        var investorNames =
            investors.ToDictionary(
                x => x.InvestorId,
                x => x.InvestorName);

        var routeByClient =
            clientRoutes.ToDictionary(
                x => x.ClientId);

        IEnumerable<ActiveLoanPortfolioItemResponse>
            scopedPortfolio =
                portfolio;

        if (investorId.HasValue)
        {
            scopedPortfolio =
                scopedPortfolio.Where(x =>
                    x.InvestorId ==
                    investorId.Value);
        }

        if (collectionRouteId.HasValue)
        {
            scopedPortfolio =
                scopedPortfolio.Where(x =>
                    routeByClient.TryGetValue(
                        x.ClientId,
                        out var clientRoute) &&
                    clientRoute.CollectionRouteId ==
                    collectionRouteId.Value);
        }

        var scopedPortfolioList =
            scopedPortfolio.ToList();

        /*
         * Delinquency incluye:
         *
         * - préstamo contractualmente vencido
         * - préstamo con mora pendiente
         *
         * Esto permite representar LateFeeOnly.
         */
        var delinquentLoans =
            scopedPortfolioList
                .Where(x =>
                    x.IsOverdue ||
                    x.HasOutstandingLateFees)
                .ToList();

        var items =
            delinquentLoans
                .Select(x =>
                    MapDelinquencyItem(
                        x,
                        investorNames,
                        routeByClient))
                .OrderByDescending(x =>
                    x.DaysOverdue)
                .ThenByDescending(x =>
                    x.TotalOverdueAmountDue)
                .ThenBy(x =>
                    x.ClientName)
                .ToList();

        var aging =
            items
                .GroupBy(x =>
                    x.AgingBucket)
                .Select(group =>
                    new FinancialDelinquencyAgingResponse
                    {
                        AgingBucket =
                            group.Key,

                        LoansCount =
                            group.Count(),

                        ClientsCount =
                            group
                                .Select(x =>
                                    x.ClientId)
                                .Distinct()
                                .Count(),

                        OverdueContractualAmount =
                            group.Sum(x =>
                                x.OverdueContractualAmount),

                        LateFeeBalanceOutstanding =
                            group.Sum(x =>
                                x.LateFeeBalanceOutstanding),

                        TotalOverdueAmountDue =
                            group.Sum(x =>
                                x.TotalOverdueAmountDue)
                    })
                .OrderBy(x =>
                    GetAgingBucketOrder(
                        x.AgingBucket))
                .ToList();

        /*
         * Summary representa el alcance completo
         * seleccionado.
         *
         * Items/Aging representan exclusivamente
         * la parte morosa de ese alcance.
         *
         * Esto permite mantener DelinquencyRate
         * consistente con el dashboard:
         *
         * overdue contractual /
         * contractual balance outstanding
         */
        var contractualBalanceOutstanding =
            scopedPortfolioList.Sum(x =>
                x.Balance);

        var overdueContractualAmount =
            delinquentLoans.Sum(x =>
                x.OverdueAmount);

        return new FinancialDelinquencyReportResponse
        {
            Summary =
                new FinancialDelinquencyReportSummaryResponse
                {
                    LoansCount =
                        scopedPortfolioList.Count,

                    ClientsCount =
                        scopedPortfolioList
                            .Select(x =>
                                x.ClientId)
                            .Distinct()
                            .Count(),

                    ContractualBalanceOutstanding =
                        contractualBalanceOutstanding,

                    LateFeeBalanceOutstanding =
                        scopedPortfolioList.Sum(x =>
                            x.LateFeeBalance),

                    TotalOutstanding =
                        scopedPortfolioList.Sum(x =>
                            x.TotalOutstanding),

                    OverdueContractualAmount =
                        overdueContractualAmount,

                    TotalOverdueAmountDue =
                        delinquentLoans.Sum(x =>
                            x.TotalOverdueAmountDue),

                    DelinquencyRate =
                        CalculateDelinquencyRate(
                            overdueContractualAmount,
                            contractualBalanceOutstanding)
                },

            Aging =
                aging,

            Items =
                items
        };
    }

    public async Task<FinancialCollectionsReportResponse>
        GetCollectionsAsync(
            Guid tenantId,
            DateOnly from,
            DateOnly to,
            Guid? investorId = null,
            Guid? collectorId = null,
            Guid? collectionRouteId = null,
            CancellationToken cancellationToken = default)
    {
        ValidateCollectionReportRequest(
            tenantId,
            from,
            to,
            investorId,
            collectorId,
            collectionRouteId);

        /*
        * Todo el filtrado histórico se ejecuta
        * directamente en PostgreSQL.
        *
        * No cargamos todos los pagos para luego
        * filtrarlos en memoria.
        */
        var collections =
            await _financialReportRepository
                .GetCollectionsAsync(
                    tenantId,
                    from,
                    to,
                    investorId,
                    collectorId,
                    collectionRouteId,
                    cancellationToken);

        var items =
            collections
                .Select(x =>
                    new FinancialCollectionsReportItemResponse
                    {
                        PaymentId =
                            x.PaymentId,

                        ReceiptNumber =
                            x.ReceiptNumber,

                        PaymentDate =
                            x.PaymentDate,

                        PaymentType =
                            x.PaymentType,

                        LoanId =
                            x.LoanId,

                        ClientId =
                            x.ClientId,

                        ClientName =
                            x.ClientName,

                        InvestorId =
                            x.InvestorId,

                        InvestorName =
                            x.InvestorName,

                        CollectedByAppUserId =
                            x.CollectedByAppUserId,

                        CollectorName =
                            x.CollectorName,

                        CollectionRouteId =
                            x.CollectionRouteId,

                        CollectionRouteName =
                            x.CollectionRouteName,

                        CashCollected =
                            x.CashCollected,

                        ContractualCashCollected =
                            x.ContractualCashCollected,

                        LateFeesCollected =
                            x.LateFeesCollected
                    })
                .ToList();

        return new FinancialCollectionsReportResponse
        {
            From =
                from,

            To =
                to,

            Summary =
                new FinancialCollectionsReportSummaryResponse
                {
                    PaymentsCount =
                        items.Count,

                    ClientsCount =
                        items
                            .Select(x =>
                                x.ClientId)
                            .Distinct()
                            .Count(),

                    CashCollected =
                        items.Sum(x =>
                            x.CashCollected),

                    ContractualCashCollected =
                        items.Sum(x =>
                            x.ContractualCashCollected),

                    LateFeesCollected =
                        items.Sum(x =>
                            x.LateFeesCollected)
                },

            Items =
                items
        };
    }

    public async Task<FinancialInvestorStatementResponse?>
        GetInvestorStatementAsync(
            Guid tenantId,
            Guid investorId,
            CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException(
                "TenantId must be a valid identifier.");
        }

        if (investorId == Guid.Empty)
        {
            throw new ArgumentException(
                "InvestorId must be a valid identifier.");
        }

        /*
        * Reutilizamos exactamente la misma fuente
        * histórica utilizada por Financial Dashboard.
        *
        * Esto evita tener dos definiciones distintas
        * para principal originado, cobros y descuentos.
        */
        var historicalData =
            await _financialDashboardRepository
                .GetInvestorHistoricalDataAsync(
                    tenantId,
                    cancellationToken);

        var investor =
            historicalData.FirstOrDefault(x =>
                x.InvestorId == investorId);

        /*
        * Al estar la consulta histórica basada en
        * Investors, la ausencia aquí significa que
        * el inversionista no pertenece al tenant.
        */
        if (investor is null)
        {
            return null;
        }

        /*
        * GetPortfolioAsync llama una sola vez a
        * GetActivePortfolioAsync y aplica el filtro
        * por InvestorId.
        *
        * Así no duplicamos cálculo de mora, balances,
        * vencimientos ni porcentaje pagado.
        */
        var portfolio =
            await GetPortfolioAsync(
                tenantId,
                investorId: investorId,
                collectionRouteId: null,
                overdueOnly: false,
                cancellationToken: cancellationToken);

        var grossContractualInterest =
            investor.ContractualAmountOriginated -
            investor.PrincipalOriginated;

        if (grossContractualInterest < 0m)
        {
            grossContractualInterest = 0m;
        }

        var netContractualInterest =
            grossContractualInterest -
            investor.EarlySettlementDiscounts;

        if (netContractualInterest < 0m)
        {
            netContractualInterest = 0m;
        }

        return new FinancialInvestorStatementResponse
        {
            InvestorId =
                investor.InvestorId,

            InvestorName =
                investor.InvestorName,

            IsActive =
                investor.IsActive,

            PrincipalOriginated =
                investor.PrincipalOriginated,

            ContractualAmountOriginated =
                investor.ContractualAmountOriginated,

            GrossContractualInterest =
                grossContractualInterest,

            EarlySettlementDiscounts =
                investor.EarlySettlementDiscounts,

            NetContractualInterest =
                netContractualInterest,

            CashCollected =
                investor.CashCollected,

            ContractualCashCollected =
                investor.ContractualCashCollected,

            LateFeesCollected =
                investor.LateFeesCollected,

            ActiveLoansCount =
                portfolio.Summary.LoansCount,

            ActiveClientsCount =
                portfolio.Summary.ClientsCount,

            ContractualBalanceOutstanding =
                portfolio.Summary
                    .ContractualBalanceOutstanding,

            LateFeeBalanceOutstanding =
                portfolio.Summary
                    .LateFeeBalanceOutstanding,

            TotalOutstanding =
                portfolio.Summary.TotalOutstanding,

            OverdueLoansCount =
                portfolio.Summary.OverdueLoansCount,

            OverdueContractualAmount =
                portfolio.Summary
                    .OverdueContractualAmount,

            DelinquencyRate =
                portfolio.Summary.DelinquencyRate,

            ActiveLoans =
                portfolio.Items
        };
    }

    private static void ValidateCollectionReportRequest(
        Guid tenantId,
        DateOnly from,
        DateOnly to,
        Guid? investorId,
        Guid? collectorId,
        Guid? collectionRouteId)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException(
                "TenantId must be a valid identifier.");
        }

        if (from > to)
        {
            throw new ArgumentException(
                "From date cannot be greater than To date.");
        }

        /*
        * Máximo 366 días inclusivos.
        *
        * Ejemplo:
        * 01-Jan a 31-Dec = válido.
        */
        var inclusiveDays =
            to.DayNumber -
            from.DayNumber +
            1;

        if (inclusiveDays > 366)
        {
            throw new ArgumentException(
                "The report period cannot exceed 366 days.");
        }

        if (
            investorId.HasValue &&
            investorId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "InvestorId must be a valid identifier.");
        }

        if (
            collectorId.HasValue &&
            collectorId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "CollectorId must be a valid identifier.");
        }

        if (
            collectionRouteId.HasValue &&
            collectionRouteId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "CollectionRouteId must be a valid identifier.");
        }
    }

    private static FinancialPortfolioReportItemResponse
        MapPortfolioItem(
            ActiveLoanPortfolioItemResponse loan,
            IReadOnlyDictionary<Guid, string> investorNames,
            IReadOnlyDictionary<
                Guid,
                FinancialReportClientRouteData>
                routeByClient)
    {
        investorNames.TryGetValue(
            loan.InvestorId,
            out var investorName);

        routeByClient.TryGetValue(
            loan.ClientId,
            out var clientRoute);

        return new FinancialPortfolioReportItemResponse
        {
            LoanId =
                loan.LoanId,

            ClientId =
                loan.ClientId,

            ClientName =
                loan.ClientName,

            ClientPhone =
                loan.ClientPhone,

            ClientAddress =
                loan.ClientAddress,

            InvestorId =
                loan.InvestorId,

            InvestorName =
                investorName ??
                string.Empty,

            CollectionRouteId =
                clientRoute?.CollectionRouteId,

            CollectionRouteName =
                clientRoute?.CollectionRouteName,

            PrincipalAmount =
                loan.PrincipalAmount,

            ContractualAmount =
                loan.TotalAmount,

            ContractualBalanceOutstanding =
                loan.Balance,

            LateFeeBalanceOutstanding =
                loan.LateFeeBalance,

            TotalOutstanding =
                loan.TotalOutstanding,

            InstallmentAmount =
                loan.InstallmentAmount,

            NextInstallmentAmountDue =
                loan.NextInstallmentAmountDue,

            NextPaymentDate =
                loan.NextPaymentDate,

            IsOverdue =
                loan.IsOverdue,

            DaysOverdue =
                loan.DaysOverdue,

            OverdueInstallments =
                loan.OverdueInstallments,

            OverdueContractualAmount =
                loan.OverdueAmount,

            TotalOverdueAmountDue =
                loan.TotalOverdueAmountDue,

            HasOutstandingLateFees =
                loan.HasOutstandingLateFees,

            PercentagePaid =
                loan.PercentagePaid,

            PaymentFrequency =
                loan.PaymentFrequency
        };
    }

    private static FinancialDelinquencyReportItemResponse
        MapDelinquencyItem(
            ActiveLoanPortfolioItemResponse loan,
            IReadOnlyDictionary<Guid, string> investorNames,
            IReadOnlyDictionary<
                Guid,
                FinancialReportClientRouteData>
                routeByClient)
    {
        investorNames.TryGetValue(
            loan.InvestorId,
            out var investorName);

        routeByClient.TryGetValue(
            loan.ClientId,
            out var clientRoute);

        return new FinancialDelinquencyReportItemResponse
        {
            LoanId =
                loan.LoanId,

            ClientId =
                loan.ClientId,

            ClientName =
                loan.ClientName,

            ClientPhone =
                loan.ClientPhone,

            InvestorId =
                loan.InvestorId,

            InvestorName =
                investorName ??
                string.Empty,

            CollectionRouteId =
                clientRoute?.CollectionRouteId,

            CollectionRouteName =
                clientRoute?.CollectionRouteName,

            ContractualBalanceOutstanding =
                loan.Balance,

            LateFeeBalanceOutstanding =
                loan.LateFeeBalance,

            TotalOutstanding =
                loan.TotalOutstanding,

            InstallmentAmount =
                loan.InstallmentAmount,

            NextInstallmentAmountDue =
                loan.NextInstallmentAmountDue,

            NextPaymentDate =
                loan.NextPaymentDate,

            DaysOverdue =
                loan.DaysOverdue,

            OverdueInstallments =
                loan.OverdueInstallments,

            OverdueContractualAmount =
                loan.OverdueAmount,

            TotalOverdueAmountDue =
                loan.TotalOverdueAmountDue,

            HasOutstandingLateFees =
                loan.HasOutstandingLateFees,

            AgingBucket =
                GetAgingBucket(
                    loan),

            PaymentFrequency =
                loan.PaymentFrequency
        };
    }

    private static FinancialPortfolioReportSummaryResponse
        BuildPortfolioSummary(
            IReadOnlyCollection<
                FinancialPortfolioReportItemResponse> items)
    {
        var contractualBalanceOutstanding =
            items.Sum(x =>
                x.ContractualBalanceOutstanding);

        var overdueContractualAmount =
            items.Sum(x =>
                x.OverdueContractualAmount);

        return new FinancialPortfolioReportSummaryResponse
        {
            LoansCount =
                items.Count,

            ClientsCount =
                items
                    .Select(x =>
                        x.ClientId)
                    .Distinct()
                    .Count(),

            PrincipalAmount =
                items.Sum(x =>
                    x.PrincipalAmount),

            ContractualAmount =
                items.Sum(x =>
                    x.ContractualAmount),

            ContractualBalanceOutstanding =
                contractualBalanceOutstanding,

            LateFeeBalanceOutstanding =
                items.Sum(x =>
                    x.LateFeeBalanceOutstanding),

            TotalOutstanding =
                items.Sum(x =>
                    x.TotalOutstanding),

            OverdueLoansCount =
                items.Count(x =>
                    x.IsOverdue),

            OverdueContractualAmount =
                overdueContractualAmount,

            TotalOverdueAmountDue =
                items.Sum(x =>
                    x.TotalOverdueAmountDue),

            DelinquencyRate =
                CalculateDelinquencyRate(
                    overdueContractualAmount,
                    contractualBalanceOutstanding)
        };
    }

    private static string GetAgingBucket(
        ActiveLoanPortfolioItemResponse loan)
    {
        /*
         * Mora pendiente sin atraso contractual actual.
         */
        if (
            !loan.IsOverdue &&
            loan.HasOutstandingLateFees)
        {
            return "LateFeeOnly";
        }

        return loan.DaysOverdue switch
        {
            <= 7 =>
                "1-7",

            <= 14 =>
                "8-14",

            <= 30 =>
                "15-30",

            <= 60 =>
                "31-60",

            _ =>
                "61+"
        };
    }

    private static int GetAgingBucketOrder(
        string bucket)
    {
        return bucket switch
        {
            "1-7" => 1,
            "8-14" => 2,
            "15-30" => 3,
            "31-60" => 4,
            "61+" => 5,
            "LateFeeOnly" => 6,
            _ => 99
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

    private static void ValidateIdentifiers(
        Guid tenantId,
        Guid? investorId,
        Guid? collectionRouteId)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException(
                "TenantId must be a valid identifier.");
        }

        if (
            investorId.HasValue &&
            investorId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "InvestorId must be a valid identifier.");
        }

        if (
            collectionRouteId.HasValue &&
            collectionRouteId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "CollectionRouteId must be a valid identifier.");
        }
    }
}