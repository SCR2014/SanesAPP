namespace Sanes.Application.FinancialReports.DTOs;

public class FinancialInvestorStatementResponse
{
    public Guid InvestorId { get; set; }

    public string InvestorName { get; set; } =
        string.Empty;

    public bool IsActive { get; set; }

    /*
     * Métricas históricas.
     */
    public decimal PrincipalOriginated { get; set; }

    public decimal ContractualAmountOriginated { get; set; }

    public decimal GrossContractualInterest { get; set; }

    public decimal EarlySettlementDiscounts { get; set; }

    public decimal NetContractualInterest { get; set; }

    public decimal CashCollected { get; set; }

    public decimal ContractualCashCollected { get; set; }

    public decimal LateFeesCollected { get; set; }

    /*
     * Estado actual de cartera.
     */
    public int ActiveLoansCount { get; set; }

    public int ActiveClientsCount { get; set; }

    public decimal ContractualBalanceOutstanding { get; set; }

    public decimal LateFeeBalanceOutstanding { get; set; }

    public decimal TotalOutstanding { get; set; }

    public int OverdueLoansCount { get; set; }

    public decimal OverdueContractualAmount { get; set; }

    public decimal DelinquencyRate { get; set; }

    /*
     * Detalle actual de préstamos activos.
     */
    public List<FinancialPortfolioReportItemResponse>
        ActiveLoans { get; set; } =
            new();
}