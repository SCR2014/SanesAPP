using Sanes.Application.FinancialReports.DTOs;

namespace Sanes.Application.FinancialReports.Exports;

public interface IFinancialReportExportService
{
    FinancialReportExportFile ExportPortfolioToExcel(
        FinancialPortfolioReportResponse report,
        FinancialReportExportContext context);

    FinancialReportExportFile ExportDelinquencyToExcel(
        FinancialDelinquencyReportResponse report,
        FinancialReportExportContext context);

    FinancialReportExportFile ExportCollectionsToExcel(
        FinancialCollectionsReportResponse report,
        FinancialReportExportContext context);

    FinancialReportExportFile ExportInvestorStatementToExcel(
        FinancialInvestorStatementResponse report,
        FinancialReportExportContext context);
}