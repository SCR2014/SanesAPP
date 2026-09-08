namespace Sanes.Application.Loans.DTOs;

public class ActivePortfolioSummaryResponse
{
    public int ActiveLoansCount { get; set; }

    public decimal TotalPrincipalAmount { get; set; }

    public decimal TotalPortfolioAmount { get; set; }

    public decimal TotalAmountPaid { get; set; }

    public decimal TotalBalance { get; set; }

    public int OverdueLoansCount { get; set; }

    public decimal TotalOverdueAmount { get; set; }

    public decimal CollectionPercentage { get; set; }
}