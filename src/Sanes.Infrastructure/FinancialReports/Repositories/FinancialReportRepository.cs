using Microsoft.EntityFrameworkCore;
using Sanes.Application.FinancialReports.Models;
using Sanes.Application.FinancialReports.Repositories;
using Sanes.Domain.Enums;
using Sanes.Infrastructure.Persistence;

namespace Sanes.Infrastructure.FinancialReports.Repositories;

public class FinancialReportRepository
    : IFinancialReportRepository
{
    private readonly SanesDbContext _dbContext;

    public FinancialReportRepository(
        SanesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<FinancialReportInvestorData>>
        GetInvestorsAsync(
            Guid tenantId,
            CancellationToken cancellationToken = default)
    {
        return await _dbContext.Investors
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId)
            .OrderBy(x =>
                x.Name)
            .Select(x =>
                new FinancialReportInvestorData
                {
                    InvestorId =
                        x.Id,

                    InvestorName =
                        x.Name,

                    IsActive =
                        x.IsActive
                })
            .ToListAsync(
                cancellationToken);
    }

    public async Task<List<FinancialReportRouteData>>
        GetRoutesAsync(
            Guid tenantId,
            CancellationToken cancellationToken = default)
    {
        return await _dbContext.CollectionRoutes
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId)
            .OrderBy(x =>
                x.Name)
            .Select(x =>
                new FinancialReportRouteData
                {
                    CollectionRouteId =
                        x.Id,

                    CollectionRouteName =
                        x.Name,

                    IsActive =
                        x.IsActive
                })
            .ToListAsync(
                cancellationToken);
    }

    public async Task<List<FinancialReportClientRouteData>>
        GetClientRoutesAsync(
            Guid tenantId,
            CancellationToken cancellationToken = default)
    {
        /*
         * No filtramos por Client.IsActive.
         *
         * La cartera activa puede conservar préstamos
         * asociados a un cliente que posteriormente
         * haya sido desactivado.
         */
        return await _dbContext.Clients
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId)
            .Select(x =>
                new FinancialReportClientRouteData
                {
                    ClientId =
                        x.Id,

                    CollectionRouteId =
                        x.CollectionRouteId,

                    CollectionRouteName =
                        x.CollectionRoute == null
                            ? null
                            : x.CollectionRoute.Name
                })
            .ToListAsync(
                cancellationToken);
    }

    public async Task<List<FinancialCollectionReportData>>
        GetCollectionsAsync(
            Guid tenantId,
            DateOnly from,
            DateOnly to,
            Guid? investorId = null,
            Guid? collectorId = null,
            Guid? collectionRouteId = null,
            CancellationToken cancellationToken = default)
    {
        var startDate =
            DateTime.SpecifyKind(
                from.ToDateTime(
                    TimeOnly.MinValue),
                DateTimeKind.Utc);

        var endDateExclusive =
            DateTime.SpecifyKind(
                to.AddDays(1)
                    .ToDateTime(
                        TimeOnly.MinValue),
                DateTimeKind.Utc);

        /*
         * Payment es la fuente primaria del reporte.
         *
         * No partimos de PaymentReceipt porque pueden
         * existir pagos históricos creados antes del
         * módulo de recibos.
         */
        var query =
            _dbContext.Payments
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.PaymentDate >= startDate &&
                    x.PaymentDate < endDateExclusive);

        if (investorId.HasValue)
        {
            query =
                query.Where(x =>
                    x.Loan.InvestorId ==
                    investorId.Value);
        }

        if (collectorId.HasValue)
        {
            query =
                query.Where(x =>
                    x.CollectedByAppUserId ==
                    collectorId.Value);
        }

        if (collectionRouteId.HasValue)
        {
            /*
             * Este filtro usa la ruta HISTÓRICA
             * guardada en Payment.
             *
             * No utiliza Client.CollectionRouteId.
             */
            query =
                query.Where(x =>
                    x.CollectionRouteId ==
                    collectionRouteId.Value);
        }

        /*
         * Proyectamos solamente los datos necesarios.
         *
         * Las allocations se agregan en PostgreSQL
         * mediante subqueries, evitando N+1.
         */
        var rows =
            await query
                .OrderBy(x =>
                    x.PaymentDate)
                .ThenBy(x =>
                    x.CreatedAt)
                .Select(x =>
                    new
                    {
                        PaymentId =
                            x.Id,

                        ReceiptNumber =
                            x.Receipt == null
                                ? null
                                : x.Receipt.ReceiptNumber,

                        x.PaymentDate,

                        x.PaymentType,

                        LoanId =
                            x.LoanId,

                        ClientId =
                            x.Loan.ClientId,

                        CurrentClientFirstName =
                            x.Loan.Client.FirstName,

                        CurrentClientLastName =
                            x.Loan.Client.LastName,

                        ReceiptClientName =
                            x.Receipt == null
                                ? null
                                : x.Receipt.ClientName,

                        InvestorId =
                            x.Loan.InvestorId,

                        InvestorName =
                            x.Loan.Investor.Name,

                        x.CollectedByAppUserId,

                        CurrentCollectorName =
                            x.CollectedByAppUser == null
                                ? null
                                : x.CollectedByAppUser.Name,

                        ReceiptCollectorName =
                            x.Receipt == null
                                ? null
                                : x.Receipt.CollectedByName,

                        x.CollectionRouteId,

                        CollectionRouteName =
                            x.CollectionRoute == null
                                ? null
                                : x.CollectionRoute.Name,

                        CashCollected =
                            x.Amount,

                        ContractualCashCollected =
                            x.Allocations
                                .Where(allocation =>
                                    allocation.AllocationType ==
                                    PaymentAllocationType
                                        .LoanBalance)
                                .Sum(allocation =>
                                    (decimal?)
                                        allocation.Amount)
                            ?? 0m,

                        LateFeesCollected =
                            x.Allocations
                                .Where(allocation =>
                                    allocation.AllocationType ==
                                    PaymentAllocationType
                                        .LateFee)
                                .Sum(allocation =>
                                    (decimal?)
                                        allocation.Amount)
                            ?? 0m
                    })
                .ToListAsync(
                    cancellationToken);

        /*
         * Construimos nombres en memoria.
         *
         * Para Client y Collector damos prioridad
         * al snapshot del Receipt cuando existe.
         */
        return rows
            .Select(x =>
            {
                var currentClientName =
                    BuildPersonName(
                        x.CurrentClientFirstName,
                        x.CurrentClientLastName);

                var clientName =
                    !string.IsNullOrWhiteSpace(
                        x.ReceiptClientName)
                        ? x.ReceiptClientName!
                        : currentClientName;

                var collectorName =
                    !string.IsNullOrWhiteSpace(
                        x.ReceiptCollectorName)
                        ? x.ReceiptCollectorName
                        : x.CurrentCollectorName;

                return new FinancialCollectionReportData
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
                        clientName,

                    InvestorId =
                        x.InvestorId,

                    InvestorName =
                        x.InvestorName,

                    CollectedByAppUserId =
                        x.CollectedByAppUserId,

                    CollectorName =
                        collectorName,

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
                };
            })
            .ToList();
    }

    private static string BuildPersonName(
        string firstName,
        string? lastName)
    {
        if (string.IsNullOrWhiteSpace(
                lastName))
        {
            return firstName.Trim();
        }

        return $"{firstName.Trim()} {lastName.Trim()}";
    }
}