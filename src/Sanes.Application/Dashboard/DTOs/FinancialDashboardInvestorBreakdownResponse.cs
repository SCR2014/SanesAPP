namespace Sanes.Application.Dashboard.DTOs;

public class FinancialDashboardInvestorBreakdownResponse
{
    public Guid InvestorId { get; set; }

    public string InvestorName { get; set; } =
        string.Empty;

    public bool IsActive { get; set; }

    // Current portfolio.
    public int ActiveLoansCount { get; set; }

    public int ActiveClientsCount { get; set; }

    // Historical origination.
    public decimal PrincipalOriginated { get; set; }

    public decimal ContractualAmountOriginated { get; set; }

    public decimal GrossContractualInterest { get; set; }

    public decimal EarlySettlementDiscounts { get; set; }

    public decimal NetContractualInterest { get; set; }

    // Current outstanding portfolio.
    public decimal ContractualBalanceOutstanding { get; set; }

    public decimal LateFeeBalanceOutstanding { get; set; }

    public decimal TotalOutstanding { get; set; }

    // Historical cash.
    public decimal CashCollected { get; set; }

    public decimal ContractualCashCollected { get; set; }

    public decimal LateFeesCollected { get; set; }

    // Delinquency.
    public int OverdueLoansCount { get; set; }

    public decimal OverdueContractualAmount { get; set; }

    public decimal DelinquencyRate { get; set; }
}