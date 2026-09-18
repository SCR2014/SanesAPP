using Sanes.Application.FinancialReports.Models;

namespace Sanes.Application.FinancialReports.Repositories;

public interface IFinancialReportRepository
{
    Task<List<FinancialReportInvestorData>>
        GetInvestorsAsync(
            Guid tenantId,
            CancellationToken cancellationToken = default);

    Task<List<FinancialReportRouteData>>
        GetRoutesAsync(
            Guid tenantId,
            CancellationToken cancellationToken = default);

    Task<List<FinancialReportClientRouteData>>
        GetClientRoutesAsync(
            Guid tenantId,
            CancellationToken cancellationToken = default);

    Task<List<FinancialCollectionReportData>>
        GetCollectionsAsync(
            Guid tenantId,
            DateOnly from,
            DateOnly to,
            Guid? investorId = null,
            Guid? collectorId = null,
            Guid? collectionRouteId = null,
            CancellationToken cancellationToken = default);
}