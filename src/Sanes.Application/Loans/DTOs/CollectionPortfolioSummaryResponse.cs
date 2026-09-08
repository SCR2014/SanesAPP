namespace Sanes.Application.Loans.DTOs;

public class CollectionPortfolioSummaryResponse
{
    public int LoansCount { get; set; }

    public int ClientsCount { get; set; }

    public decimal TotalBalance { get; set; }

    public decimal TotalNextInstallmentAmountDue { get; set; }

    public int OverdueLoansCount { get; set; }

    public decimal TotalOverdueAmount { get; set; }
}