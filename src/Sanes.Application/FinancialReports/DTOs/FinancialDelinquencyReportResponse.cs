namespace Sanes.Application.FinancialReports.DTOs;

public class FinancialDelinquencyReportResponse
{
    public FinancialDelinquencyReportSummaryResponse Summary { get; set; } =
        new();

    public List<FinancialDelinquencyAgingResponse> Aging { get; set; } =
        new();

    public List<FinancialDelinquencyReportItemResponse> Items { get; set; } =
        new();
}