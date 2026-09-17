using Microsoft.EntityFrameworkCore;
using Sanes.Application.Dashboard.Models;
using Sanes.Application.Dashboard.Repositories;
using Sanes.Domain.Enums;
using Sanes.Infrastructure.Persistence;

namespace Sanes.Infrastructure.Dashboard.Repositories;

public class FinancialDashboardRepository
    : IFinancialDashboardRepository
{
    private readonly SanesDbContext _dbContext;

    public FinancialDashboardRepository(
        SanesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<FinancialDashboardHistoricalData>
        GetHistoricalDataAsync(
            Guid tenantId,
            CancellationToken cancellationToken = default)
    {
        /*
         * Originación histórica.
         *
         * Usamos todos los préstamos del Tenant,
         * independientemente de que actualmente estén
         * Active, Paid o Cancelled.
         *
         * Esta métrica representa lo que históricamente
         * fue originado, no la cartera actual.
         */
        var principalOriginated =
            await _dbContext.Loans
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId)
                .SumAsync(
                    x => (decimal?)x.PrincipalAmount,
                    cancellationToken)
                ?? 0m;

        var contractualAmountOriginated =
            await _dbContext.Loans
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId)
                .SumAsync(
                    x =>
                        (decimal?)(
                            x.InstallmentAmount *
                            x.TotalInstallments),
                    cancellationToken)
                ?? 0m;

        /*
         * Dinero efectivamente recibido.
         *
         * No usamos PaymentReceipt porque existen
         * pagos históricos anteriores a esa feature.
         */
        var cashCollected =
            await _dbContext.Payments
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId)
                .SumAsync(
                    x => (decimal?)x.Amount,
                    cancellationToken)
                ?? 0m;

        var contractualCashCollected =
            await _dbContext.PaymentAllocations
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.AllocationType ==
                        PaymentAllocationType.LoanBalance)
                .SumAsync(
                    x => (decimal?)x.Amount,
                    cancellationToken)
                ?? 0m;

        var lateFeesCollected =
            await _dbContext.PaymentAllocations
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.AllocationType ==
                        PaymentAllocationType.LateFee)
                .SumAsync(
                    x => (decimal?)x.Amount,
                    cancellationToken)
                ?? 0m;

        var earlySettlementDiscounts =
            await _dbContext.LoanBalanceAdjustments
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.AdjustmentType ==
                        LoanBalanceAdjustmentType
                            .EarlySettlementDiscount)
                .SumAsync(
                    x => (decimal?)x.Amount,
                    cancellationToken)
                ?? 0m;

        return new FinancialDashboardHistoricalData
        {
            PrincipalOriginated =
                principalOriginated,

            ContractualAmountOriginated =
                contractualAmountOriginated,

            EarlySettlementDiscounts =
                earlySettlementDiscounts,

            CashCollected =
                cashCollected,

            ContractualCashCollected =
                contractualCashCollected,

            LateFeesCollected =
                lateFeesCollected
        };
    }

    public async Task<List<FinancialDashboardCashFlowDataItem>>
        GetCashFlowAsync(
            Guid tenantId,
            DateOnly from,
            DateOnly to,
            CancellationToken cancellationToken = default)
    {
        var fromUtc =
            DateTime.SpecifyKind(
                from.ToDateTime(
                    TimeOnly.MinValue),
                DateTimeKind.Utc);

        /*
         * Intervalo semiabierto:
         *
         * [from, to + 1 día)
         *
         * evita problemas con 23:59:59.999...
         */
        var toExclusiveUtc =
            DateTime.SpecifyKind(
                to.AddDays(1)
                    .ToDateTime(
                        TimeOnly.MinValue),
                DateTimeKind.Utc);

        // ========================================================
        // ORIGINATIONS
        // ========================================================

        var originations =
            await _dbContext.Loans
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.CreatedAt >= fromUtc &&
                    x.CreatedAt < toExclusiveUtc)
                .GroupBy(x =>
                    x.CreatedAt.Date)
                .Select(group =>
                    new
                    {
                        Date =
                            group.Key,

                        Amount =
                            group.Sum(x =>
                                x.PrincipalAmount)
                    })
                .ToListAsync(
                    cancellationToken);

        // ========================================================
        // CASH RECEIVED
        // ========================================================

        var payments =
            await _dbContext.Payments
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.PaymentDate >= fromUtc &&
                    x.PaymentDate < toExclusiveUtc)
                .GroupBy(x =>
                    x.PaymentDate.Date)
                .Select(group =>
                    new
                    {
                        Date =
                            group.Key,

                        Amount =
                            group.Sum(x =>
                                x.Amount)
                    })
                .ToListAsync(
                    cancellationToken);

        // ========================================================
        // PAYMENT ALLOCATIONS
        // ========================================================

        var contractualCollections =
            await _dbContext.PaymentAllocations
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.AllocationType ==
                        PaymentAllocationType.LoanBalance &&
                    x.Payment.PaymentDate >= fromUtc &&
                    x.Payment.PaymentDate < toExclusiveUtc)
                .GroupBy(x =>
                    x.Payment.PaymentDate.Date)
                .Select(group =>
                    new
                    {
                        Date =
                            group.Key,

                        Amount =
                            group.Sum(x =>
                                x.Amount)
                    })
                .ToListAsync(
                    cancellationToken);

        var lateFeeCollections =
            await _dbContext.PaymentAllocations
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.AllocationType ==
                        PaymentAllocationType.LateFee &&
                    x.Payment.PaymentDate >= fromUtc &&
                    x.Payment.PaymentDate < toExclusiveUtc)
                .GroupBy(x =>
                    x.Payment.PaymentDate.Date)
                .Select(group =>
                    new
                    {
                        Date =
                            group.Key,

                        Amount =
                            group.Sum(x =>
                                x.Amount)
                    })
                .ToListAsync(
                    cancellationToken);

        // ========================================================
        // EARLY SETTLEMENT DISCOUNTS
        // ========================================================

        var discounts =
            await _dbContext.LoanBalanceAdjustments
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.AdjustmentType ==
                        LoanBalanceAdjustmentType
                            .EarlySettlementDiscount &&
                    x.CreatedAt >= fromUtc &&
                    x.CreatedAt < toExclusiveUtc)
                .GroupBy(x =>
                    x.CreatedAt.Date)
                .Select(group =>
                    new
                    {
                        Date =
                            group.Key,

                        Amount =
                            group.Sum(x =>
                                x.Amount)
                    })
                .ToListAsync(
                    cancellationToken);

        // ========================================================
        // MERGE
        // ========================================================

        var dates =
            originations
                .Select(x => x.Date)
                .Concat(
                    payments.Select(x =>
                        x.Date))
                .Concat(
                    contractualCollections.Select(x =>
                        x.Date))
                .Concat(
                    lateFeeCollections.Select(x =>
                        x.Date))
                .Concat(
                    discounts.Select(x =>
                        x.Date))
                .Distinct()
                .OrderBy(x => x)
                .ToList();

        var result =
            new List<
                FinancialDashboardCashFlowDataItem>();

        foreach (var date in dates)
        {
            result.Add(
                new FinancialDashboardCashFlowDataItem
                {
                    Date =
                        DateOnly.FromDateTime(
                            date),

                    PrincipalOriginated =
                        originations
                            .Where(x =>
                                x.Date == date)
                            .Sum(x =>
                                x.Amount),

                    CashCollected =
                        payments
                            .Where(x =>
                                x.Date == date)
                            .Sum(x =>
                                x.Amount),

                    ContractualCashCollected =
                        contractualCollections
                            .Where(x =>
                                x.Date == date)
                            .Sum(x =>
                                x.Amount),

                    LateFeesCollected =
                        lateFeeCollections
                            .Where(x =>
                                x.Date == date)
                            .Sum(x =>
                                x.Amount),

                    EarlySettlementDiscounts =
                        discounts
                            .Where(x =>
                                x.Date == date)
                            .Sum(x =>
                                x.Amount)
                });
        }

        return result;
    }
}