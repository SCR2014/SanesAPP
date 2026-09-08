using Sanes.Domain.Enums;

namespace Sanes.Application.Loans.DTOs;

public class CollectionLoanItemResponse
{
    public Guid LoanId { get; set; }

    public Guid ClientId { get; set; }

    public string ClientName { get; set; } = string.Empty;

    public string ClientPhone { get; set; } = string.Empty;

    public string? ClientAddress { get; set; }

    public decimal Balance { get; set; }

    public decimal InstallmentAmount { get; set; }

    public decimal NextInstallmentAmountDue { get; set; }

    public DateTime NextPaymentDate { get; set; }

    public bool IsOverdue { get; set; }

    public int DaysOverdue { get; set; }

    public int OverdueInstallments { get; set; }

    public decimal OverdueAmount { get; set; }

    public decimal PercentagePaid { get; set; }

    public PaymentFrequency PaymentFrequency { get; set; }
}