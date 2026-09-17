namespace Sanes.Application.Dashboard.Models;

public class FinancialDashboardInvestorHistoricalData
{
    public Guid InvestorId { get; set; }

    public string InvestorName { get; set; } =
        string.Empty;

    public bool IsActive { get; set; }

    public decimal PrincipalOriginated { get; set; }

    public decimal ContractualAmountOriginated { get; set; }

    public decimal EarlySettlementDiscounts { get; set; }

    public decimal CashCollected { get; set; }

    public decimal ContractualCashCollected { get; set; }

    public decimal LateFeesCollected { get; set; }
}