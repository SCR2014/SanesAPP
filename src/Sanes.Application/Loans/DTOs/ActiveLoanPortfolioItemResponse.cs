using Sanes.Domain.Enums;

namespace Sanes.Application.Loans.DTOs;

public class ActiveLoanPortfolioItemResponse
{
    public Guid LoanId { get; set; }

    public Guid InvestorId { get; set; }

    public Guid ClientId { get; set; }

    public string ClientName { get; set; } = string.Empty;

    public string ClientPhone { get; set; } = string.Empty;

    public string? ClientAddress { get; set; }
    public decimal PrincipalAmount { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal AmountPaid { get; set; }

    public decimal Balance { get; set; }

    public decimal InstallmentAmount { get; set; }

    public int TotalInstallments { get; set; }

    public int CompletedInstallments { get; set; }

    public int RemainingInstallments { get; set; }

    public decimal NextInstallmentAmountDue { get; set; }

    public bool IsOverdue { get; set; }

    public int DaysOverdue { get; set; }

    public int OverdueInstallments { get; set; }

    public decimal OverdueAmount { get; set; }

    public decimal PercentagePaid { get; set; }

    public PaymentFrequency PaymentFrequency { get; set; }

    public DateTime NextPaymentDate { get; set; }

    public LoanStatus Status { get; set; }
}