using Sanes.Application.FinancialReports.DTOs;

namespace Sanes.Web.FinancialReports;

public interface IFinancialReportsWebService
{
    Task<FinancialReportsSetup> GetSetupAsync(
        CancellationToken cancellationToken = default);

    Task<FinancialPortfolioReportResponse> GetPortfolioAsync(
        Guid? investorId,
        Guid? collectionRouteId,
        bool overdueOnly,
        CancellationToken cancellationToken = default);

    Task<FinancialDelinquencyReportResponse>
        GetDelinquencyAsync(
            Guid? investorId,
            Guid? collectionRouteId,
            CancellationToken cancellationToken = default);

    Task<FinancialCollectionsReportResponse>
        GetCollectionsAsync(
            DateOnly from,
            DateOnly to,
            Guid? investorId,
            Guid? collectorId,
            Guid? collectionRouteId,
            CancellationToken cancellationToken = default);

    Task<FinancialInvestorStatementResponse?>
        GetInvestorStatementAsync(
            Guid investorId,
            CancellationToken cancellationToken = default);

    Task<FinancialReportDownload>
        ExportPortfolioAsync(
            Guid? investorId,
            Guid? collectionRouteId,
            bool overdueOnly,
            CancellationToken cancellationToken = default);

    Task<FinancialReportDownload>
        ExportDelinquencyAsync(
            Guid? investorId,
            Guid? collectionRouteId,
            CancellationToken cancellationToken = default);

    Task<FinancialReportDownload>
        ExportCollectionsAsync(
            DateOnly from,
            DateOnly to,
            Guid? investorId,
            Guid? collectorId,
            Guid? collectionRouteId,
            CancellationToken cancellationToken = default);

    Task<FinancialReportDownload>
        ExportInvestorStatementAsync(
            Guid investorId,
            CancellationToken cancellationToken = default);
}