namespace Sanes.Application.FinancialReports.DTOs;

public class FinancialPortfolioReportSummaryResponse
{
    public int LoansCount { get; set; }

    public int ClientsCount { get; set; }

    public decimal PrincipalAmount { get; set; }

    public decimal ContractualAmount { get; set; }

    public decimal ContractualBalanceOutstanding { get; set; }

    public decimal LateFeeBalanceOutstanding { get; set; }

    public decimal TotalOutstanding { get; set; }

    public int OverdueLoansCount { get; set; }

    public decimal OverdueContractualAmount { get; set; }

    public decimal TotalOverdueAmountDue { get; set; }

    public decimal DelinquencyRate { get; set; }
}