namespace Sanes.Application.FinancialReports.DTOs;

public class FinancialCollectionsReportSummaryResponse
{
    public int PaymentsCount { get; set; }

    public int ClientsCount { get; set; }

    public decimal CashCollected { get; set; }

    public decimal ContractualCashCollected { get; set; }

    public decimal LateFeesCollected { get; set; }
}