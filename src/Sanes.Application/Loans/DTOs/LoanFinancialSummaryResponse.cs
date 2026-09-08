using Sanes.Domain.Enums;

namespace Sanes.Application.Loans.DTOs;

public class LoanFinancialSummaryResponse
{
    public Guid LoanId { get; set; }

    public decimal PrincipalAmount { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal InterestAmount { get; set; }

    public decimal AmountPaid { get; set; }

    public decimal Balance { get; set; }

    public decimal InstallmentAmount { get; set; }

    public int TotalInstallments { get; set; }

    public int CompletedInstallments { get; set; }

    public int RemainingInstallments { get; set; }

    public decimal CurrentInstallmentPaidAmount { get; set; }

    public decimal CurrentInstallmentRemainingAmount { get; set; }

    public decimal NextInstallmentAmountDue { get; set; }

    public decimal PercentagePaid { get; set; }

    public bool IsOverdue { get; set; }

    public int DaysOverdue { get; set; }

    public int OverdueInstallments { get; set; }

    public decimal OverdueAmount { get; set; }

    public PaymentFrequency PaymentFrequency { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime NextPaymentDate { get; set; }

    public LoanStatus Status { get; set; }
}