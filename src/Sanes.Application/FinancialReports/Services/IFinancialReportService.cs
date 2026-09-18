using Sanes.Application.FinancialReports.DTOs;

namespace Sanes.Application.FinancialReports.Services;

public interface IFinancialReportService
{
    Task<FinancialPortfolioReportResponse>
        GetPortfolioAsync(
            Guid tenantId,
            Guid? investorId = null,
            Guid? collectionRouteId = null,
            bool overdueOnly = false,
            CancellationToken cancellationToken = default);

    Task<FinancialDelinquencyReportResponse>
        GetDelinquencyAsync(
            Guid tenantId,
            Guid? investorId = null,
            Guid? collectionRouteId = null,
            CancellationToken cancellationToken = default);

    Task<FinancialCollectionsReportResponse>
        GetCollectionsAsync(
            Guid tenantId,
            DateOnly from,
            DateOnly to,
            Guid? investorId = null,
            Guid? collectorId = null,
            Guid? collectionRouteId = null,
            CancellationToken cancellationToken = default);
    
    Task<FinancialInvestorStatementResponse?>
    GetInvestorStatementAsync(
        Guid tenantId,
        Guid investorId,
        CancellationToken cancellationToken = default);
}