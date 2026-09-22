namespace Sanes.Application.FinancialReports.DTOs;

public class FinancialCollectionsReportResponse
{
    public DateOnly From { get; set; }

    public DateOnly To { get; set; }

    public FinancialCollectionsReportSummaryResponse Summary { get; set; } =
        new();

    public List<FinancialCollectionsReportItemResponse> Items { get; set; } =
        new();
}