namespace Sanes.Application.FinancialReports.DTOs;

public class FinancialDelinquencyAgingResponse
{
    public string AgingBucket { get; set; } =
        string.Empty;

    public int LoansCount { get; set; }

    public int ClientsCount { get; set; }

    public decimal OverdueContractualAmount { get; set; }

    public decimal LateFeeBalanceOutstanding { get; set; }

    public decimal TotalOverdueAmountDue { get; set; }
}