namespace Sanes.Application.FinancialReports.DTOs;

public class FinancialPortfolioReportResponse
{
    public FinancialPortfolioReportSummaryResponse Summary { get; set; } =
        new();

    public List<FinancialPortfolioReportItemResponse> Items { get; set; } =
        new();
}