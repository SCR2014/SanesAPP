namespace Sanes.Application.Dashboard.DTOs;

public class FinancialDashboardCashFlowResponse
{
    public DateOnly From { get; set; }

    public DateOnly To { get; set; }

    /*
     * Totals for the requested period.
     */
    public decimal PrincipalOriginated { get; set; }

    public decimal CashCollected { get; set; }

    public decimal ContractualCashCollected { get; set; }

    public decimal LateFeesCollected { get; set; }

    public decimal EarlySettlementDiscounts { get; set; }

    public List<FinancialDashboardCashFlowItemResponse>
        Items { get; set; } =
        new();
}