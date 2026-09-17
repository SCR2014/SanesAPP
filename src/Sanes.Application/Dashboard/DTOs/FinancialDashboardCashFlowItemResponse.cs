namespace Sanes.Application.Dashboard.DTOs;

public class FinancialDashboardCashFlowItemResponse
{
    public DateOnly Date { get; set; }

    /*
     * New principal originated on this date.
     */
    public decimal PrincipalOriginated { get; set; }

    /*
     * Actual cash received.
     */
    public decimal CashCollected { get; set; }

    /*
     * Cash applied to contractual loan balance.
     */
    public decimal ContractualCashCollected { get; set; }

    /*
     * Cash applied to late fees.
     */
    public decimal LateFeesCollected { get; set; }

    /*
     * Contractual reductions from
     * early settlements.
     */
    public decimal EarlySettlementDiscounts { get; set; }
}