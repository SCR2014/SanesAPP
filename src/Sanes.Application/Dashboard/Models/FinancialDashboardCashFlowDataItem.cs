namespace Sanes.Application.Dashboard.Models;

public class FinancialDashboardCashFlowDataItem
{
    public DateOnly Date { get; set; }

    public decimal PrincipalOriginated { get; set; }

    public decimal CashCollected { get; set; }

    public decimal ContractualCashCollected { get; set; }

    public decimal LateFeesCollected { get; set; }

    public decimal EarlySettlementDiscounts { get; set; }
}